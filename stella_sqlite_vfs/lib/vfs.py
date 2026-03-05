"""
Virtual File System backed by SQLite3.

This module provides a file system abstraction that stores all files and directories
in a SQLite database, split into chunks for memory efficiency. It implements standard
Python IO interfaces.

Example:
    from lib.vfs import VirtualFileSystem

    vfs = VirtualFileSystem("myfiles.db")

    # Write a file
    with vfs.open("/docs/readme.txt", "w") as f:
        f.write("Hello, World!")

    # Read a file
    with vfs.open("/docs/readme.txt", "r") as f:
        content = f.read()
"""

import sqlite3
import io
import posixpath
import builtins
import stat as stat_module
import threading
from contextlib import contextmanager
from datetime import datetime
from typing import Optional, Union, List, Iterator, Tuple, NamedTuple, Callable

from icontract import require, ensure, invariant, DBC


class StatResult(NamedTuple):
    """Stat result compatible with os.stat() interface."""
    st_size: int
    st_ctime: str  # ISO format timestamp
    st_mtime: str  # ISO format timestamp  
    st_mode: int
    source_layer: str = 'local'
    
    # Convenience aliases for dict-style access compatibility
    @property
    def size(self) -> int:
        return self.st_size
    
    @property
    def created_at(self) -> str:
        return self.st_ctime
    
    @property
    def modified_at(self) -> str:
        return self.st_mtime
    
    @property
    def is_directory(self) -> bool:
        return stat_module.S_ISDIR(self.st_mode)


class VirtualFileSystemError(Exception):
    """Base exception for VFS errors."""
    pass


# Inheriting from builtins ensures compatibility with 'io' module and standard try/except blocks
class FileNotFoundError(VirtualFileSystemError, builtins.FileNotFoundError):
    """Raised when a file or directory is not found."""
    pass


class FileExistsError(VirtualFileSystemError, builtins.FileExistsError):
    """Raised when a file or directory already exists."""
    pass


class IsADirectoryError(VirtualFileSystemError, builtins.IsADirectoryError):
    """Raised when a directory operation is attempted on a file."""
    pass


class NotADirectoryError(VirtualFileSystemError, builtins.NotADirectoryError):
    """Raised when a file operation is attempted on a directory."""
    pass


class PermissionError(VirtualFileSystemError, builtins.PermissionError):
    """Raised when operation is not permitted."""
    pass


# Valid file modes including exclusive creation ('x')
VALID_FILE_MODES = frozenset({
    'r', 'w', 'a', 'x',
    'rb', 'wb', 'ab', 'xb',
    'r+', 'w+', 'a+', 'x+',
    'rb+', 'wb+', 'ab+', 'xb+',
    'r+b', 'w+b', 'a+b', 'x+b',
})


@invariant(lambda self: self._pos >= 0, "Position must be non-negative")
@invariant(lambda self: self._size >= 0, "Size must be non-negative")
class VirtualFileRaw(io.RawIOBase):
    """
    A RawIOBase implementation that reads/writes directly to SQLite chunks.
    This provides the low-level byte stream interface.
    """
    DEFAULT_CHUNK_SIZE = 65536  # 64KB chunks

    @require(lambda vfs: vfs is not None, "VFS instance required")
    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @require(lambda mode: mode in VALID_FILE_MODES, "Invalid file mode")
    def __init__(self, vfs: 'VirtualFileSystem', path: str, mode: str):
        self._vfs = vfs
        self._path = path
        self._mode = mode
        self._chunk_size = vfs._chunk_size
        self.name = path  # Required by some io wrappers
        self.mode = mode  # Required by some io wrappers
        self._pos = 0
        self._inode_id = None
        self._size = 0
        
        # Parse mode flags
        self._writable = 'w' in mode or 'a' in mode or 'x' in mode or '+' in mode
        self._readable = 'r' in mode or '+' in mode
        self._append = 'a' in mode
        self._exclusive = 'x' in mode  # Fail if file exists
        
        self._load_inode()

        # Handle truncation for 'w' mode (but not 'r+' or 'w+')
        if 'w' in mode and not '+' in mode:
            self.truncate(0)
        
        # Handle append positioning
        if self._append:
            self._pos = self._size

    def _load_inode(self):
        """Load metadata from the DB or create if necessary.
        
        Thread-safe: handles race conditions where another thread may create
        the same file between our check and our insert.
        """
        with self._vfs._cursor() as cursor:
            cursor.execute('SELECT id, size, is_directory FROM inodes WHERE path = ?', (self._path,))
            row = cursor.fetchone()
            
            if row:
                if row['is_directory']:
                    raise IsADirectoryError(f"Is a directory: {self._path}")
                if self._exclusive:
                    raise FileExistsError(f"File exists (exclusive mode): {self._path}")
                self._inode_id = row['id']
                self._size = row['size']
                return
        
        # File doesn't exist - check if we can create it
        if not self._writable:
            raise FileNotFoundError(f"File not found: {self._path}")
        
        # Create with conflict handling (fixes TOCTOU race)
        with self._vfs._transaction() as cursor:
            now = datetime.now().isoformat()
            try:
                cursor.execute(
                    'INSERT INTO inodes (path, is_directory, created_at, modified_at, size) VALUES (?, 0, ?, ?, 0)',
                    (self._path, now, now)
                )
                self._inode_id = cursor.lastrowid
                self._size = 0
            except sqlite3.IntegrityError:
                # Race condition: another thread created it - fetch the existing one
                cursor.execute('SELECT id, size, is_directory FROM inodes WHERE path = ?', (self._path,))
                row = cursor.fetchone()
                if row is None:
                    raise  # Something else went wrong
                if row['is_directory']:
                    raise IsADirectoryError(f"Is a directory: {self._path}")
                if self._exclusive:
                    raise FileExistsError(f"File exists (exclusive mode): {self._path}")
                self._inode_id = row['id']
                self._size = row['size']

    def readable(self) -> bool:
        return self._readable

    def writable(self) -> bool:
        return self._writable

    def seekable(self) -> bool:
        return True

    @ensure(lambda result: result >= 0, "Position must be non-negative")
    def tell(self) -> int:
        return self._pos

    @ensure(lambda result: result >= 0, "Resulting position must be non-negative")
    def seek(self, offset: int, whence: int = 0) -> int:
        if whence == 0:  # SEEK_SET
            self._pos = offset
        elif whence == 1:  # SEEK_CUR
            self._pos += offset
        elif whence == 2:  # SEEK_END
            self._pos = self._size + offset
        
        self._pos = max(0, self._pos)
        return self._pos

    @require(lambda b: b is not None, "Buffer must not be None")
    @ensure(lambda result: result is None or result >= 0, "Bytes read must be non-negative")
    def readinto(self, b: bytearray) -> Optional[int]:
        """
        Read up to len(b) bytes into bytearray b.
        Returns number of bytes read.
        
        Memory-safe: streams chunks one at a time instead of loading all into RAM.
        """
        if not self._readable:
            raise io.UnsupportedOperation("File not open for reading")

        if self._pos >= self._size:
            return 0

        # Important: Don't read past the actual file size
        # This prevents filling the buffer with zeros from "phantom" chunks
        remaining_file = self._size - self._pos
        size_to_read = min(len(b), remaining_file)
        
        if size_to_read <= 0:
            return 0

        start_chunk = self._pos // self._chunk_size
        end_chunk = (self._pos + size_to_read - 1) // self._chunk_size
        chunk_offset = self._pos % self._chunk_size

        bytes_read = 0
        buffer_idx = 0

        with self._vfs._cursor() as cursor:
            # Stream chunks one by one to avoid memory explosion on large files
            cursor.execute('''
                SELECT chunk_index, data FROM chunks 
                WHERE inode_id = ? AND chunk_index BETWEEN ? AND ?
                ORDER BY chunk_index ASC
            ''', (self._inode_id, start_chunk, end_chunk))
            
            # Use a look-ahead iterator to handle sparse chunks
            current_row = cursor.fetchone()
            
            for i in range(start_chunk, end_chunk + 1):
                # Check if we have data for this chunk index
                chunk_data = b''
                if current_row and current_row['chunk_index'] == i:
                    chunk_data = current_row['data']
                    current_row = cursor.fetchone()  # Advance to next row
                
                # Sparse file handling: if chunk missing but within size, treat as zeros
                if not chunk_data:
                    chunk_data = b'\x00' * self._chunk_size

                # Determine start and end within this specific chunk
                start_in_chunk = chunk_offset if i == start_chunk else 0
                
                # How much do we need from this chunk?
                remaining_request = size_to_read - bytes_read
                available_in_chunk = len(chunk_data) - start_in_chunk
                
                bytes_to_copy = min(remaining_request, available_in_chunk)
                
                # Copy data
                b[buffer_idx : buffer_idx + bytes_to_copy] = chunk_data[start_in_chunk : start_in_chunk + bytes_to_copy]
                
                bytes_read += bytes_to_copy
                buffer_idx += bytes_to_copy
                
                if bytes_read >= size_to_read:
                    break
        
        self._pos += bytes_read
        return bytes_read

    @require(lambda b: b is not None, "Data must not be None")
    @ensure(lambda self, b, result: result == len(b), "All bytes must be written")
    def write(self, b: bytes) -> int:
        """
        Write bytes to the file at the current position.
        Handles partial chunk updates and file growth.
        """
        if not self._writable:
            raise io.UnsupportedOperation("File not open for writing")

        total_len = len(b)
        if total_len == 0:
            return 0

        with self._vfs._transaction() as cursor:
            start_pos = self._pos
            end_pos = start_pos + total_len
            
            start_chunk_idx = start_pos // self._chunk_size
            end_chunk_idx = (end_pos - 1) // self._chunk_size
            
            data_offset = 0
            
            for chunk_idx in range(start_chunk_idx, end_chunk_idx + 1):
                chunk_start_abs = chunk_idx * self._chunk_size
                write_start_rel = max(0, start_pos - chunk_start_abs)
                amount_to_write = min(total_len - data_offset, self._chunk_size - write_start_rel)
                
                # Get data slice to write
                new_part = b[data_offset : data_offset + amount_to_write]
                
                # If we are doing a partial overwrite, we need the existing chunk
                need_existing = (write_start_rel > 0) or (amount_to_write < self._chunk_size)
                
                existing_data = b''
                if need_existing:
                    cursor.execute('SELECT data FROM chunks WHERE inode_id = ? AND chunk_index = ?', 
                                   (self._inode_id, chunk_idx))
                    row = cursor.fetchone()
                    if row:
                        existing_data = row['data']
                
                # Pad with zeros if writing past end of current data (sparse write)
                if len(existing_data) < write_start_rel:
                    existing_data += b'\x00' * (write_start_rel - len(existing_data))
                    
                # Construct final chunk
                prefix = existing_data[:write_start_rel]
                write_end_rel = write_start_rel + amount_to_write
                suffix = existing_data[write_end_rel:]
                
                final_chunk_data = prefix + new_part + suffix
                
                cursor.execute('''
                    INSERT OR REPLACE INTO chunks (inode_id, chunk_index, data) 
                    VALUES (?, ?, ?)
                ''', (self._inode_id, chunk_idx, final_chunk_data))
                
                data_offset += amount_to_write
                
            # Update metadata (commit handled by _transaction context manager)
            new_size = max(self._size, end_pos)
            now = datetime.now().isoformat()
            cursor.execute('UPDATE inodes SET size = ?, modified_at = ? WHERE id = ?', 
                           (new_size, now, self._inode_id))
        
        self._size = new_size
        self._pos += total_len
        return total_len

    def flush(self) -> None:
        """Flush write buffers to the database."""
        if self._writable:
            with self._vfs._lock:
                self._vfs._conn.commit()

    @ensure(lambda result: result >= 0, "Resulting size must be non-negative")
    def truncate(self, size: Optional[int] = None) -> int:
        if not self._writable:
             raise io.UnsupportedOperation("File not open for writing")
        
        if size is None:
            size = self._pos
        if size < 0:
            raise ValueError("Negative size")

        with self._vfs._transaction() as cursor:
            # 1. Update inode size
            now = datetime.now().isoformat()
            cursor.execute('UPDATE inodes SET size = ?, modified_at = ? WHERE id = ?', 
                           (size, now, self._inode_id))

            if size == 0:
                cursor.execute('DELETE FROM chunks WHERE inode_id = ?', (self._inode_id,))
            else:
                # 2. Delete chunks completely beyond new size
                # Chunk i contains bytes [i*CHUNK, (i+1)*CHUNK)
                # We keep chunk if its start < size
                max_keep_chunk = (size - 1) // self._chunk_size
                cursor.execute('DELETE FROM chunks WHERE inode_id = ? AND chunk_index > ?', 
                               (self._inode_id, max_keep_chunk))
                
                # 3. Trim the last chunk
                offset_in_chunk = size % self._chunk_size
                if offset_in_chunk > 0:
                    cursor.execute('SELECT data FROM chunks WHERE inode_id = ? AND chunk_index = ?',
                                   (self._inode_id, max_keep_chunk))
                    row = cursor.fetchone()
                    if row:
                        data = row['data']
                        if len(data) > offset_in_chunk:
                            new_data = data[:offset_in_chunk]
                            cursor.execute('UPDATE chunks SET data = ? WHERE inode_id = ? AND chunk_index = ?',
                                           (new_data, self._inode_id, max_keep_chunk))
        
        self._size = size
        # POSIX: "The current file position is not changed."
        return size

    def close(self) -> None:
        """Close the file, committing any pending writes."""
        if not self.closed:
            if self._writable:
                self.flush()
            super().close()

    def __del__(self):
        """Ensure file is closed on garbage collection."""
        try:
            self.close()
        except Exception:
            pass  # Avoid exceptions in __del__


@invariant(
    lambda self: not hasattr(self, '_closed') or self._conn is not None or self._closed,
    "Database connection must be open (unless closed or initializing)"
)
@invariant(
    lambda self: not hasattr(self, '_closed') or self._lock is not None or self._closed,
    "Lock must be initialized (unless closed or initializing)"
)
class VirtualFileSystem(DBC):
    """
    A virtual file system backed by SQLite3.
    Uses chunked storage and standard IO interfaces.
    
    Args:
        db_path: Path to SQLite database file, or ":memory:" for in-memory DB
        chunk_size: Size of file chunks in bytes (default: 64KB)
        read_only: If True, opens database in read-only mode
    
    Supports optional layering/overlay for mod and DLC support:
        # Base game (read-only)
        base = VirtualFileSystem("base_game.db", read_only=True)
        
        # Game with mod overlay
        game = VirtualFileSystem("user_mod.db")
        game.add_underlay(base, priority=0, name="base")
        
        # Reading checks local first, then underlays by priority
        # Writing always goes to the local (top) layer
    """
    
    DEFAULT_CHUNK_SIZE = 65536  # 64KB

    @require(lambda db_path: db_path is not None and len(db_path) > 0, "Database path must not be empty")
    @require(lambda chunk_size: chunk_size is None or chunk_size > 0, "Chunk size must be positive")
    def __init__(self, db_path: str = ":memory:", chunk_size: int = None, read_only: bool = False):
        # Initialize sentinel fields first (for __del__ safety if __init__ fails)
        self._closed = False
        self._conn = None
        self._lock = None
        
        self._db_path = db_path
        self._chunk_size = chunk_size or self.DEFAULT_CHUNK_SIZE
        self._read_only = read_only
        
        # Open database (read-only mode uses URI)
        if read_only and db_path != ":memory:":
            uri = f"file:{db_path}?mode=ro"
            self._conn = sqlite3.connect(uri, uri=True, check_same_thread=False)
        else:
            self._conn = sqlite3.connect(db_path, check_same_thread=False)
        
        self._conn.row_factory = sqlite3.Row
        self._lock = threading.RLock()  # Re-entrant lock for thread safety
        
        # Must be set per-connection (not persisted in DB)
        # Note: Does not validate existing data, only enforces on new operations
        self._conn.execute("PRAGMA foreign_keys = ON")
        
        # Underlay support for mod/DLC layering
        self._underlays: List[Tuple[int, str, 'VirtualFileSystem']] = []
        self._underlay_by_name: dict = {}
        
        if not read_only:
            self._init_schema()

    @contextmanager
    def _cursor(self):
        """Thread-safe cursor context manager with proper cleanup."""
        with self._lock:
            cursor = self._conn.cursor()
            try:
                yield cursor
            finally:
                cursor.close()

    @contextmanager
    def _transaction(self):
        """Thread-safe transaction context manager with automatic commit/rollback.
        
        Usage:
            with self._transaction() as cursor:
                cursor.execute(...)
            # Auto-commits on success, auto-rollbacks on exception
        """
        with self._lock:
            cursor = self._conn.cursor()
            try:
                yield cursor
                self._conn.commit()
            except Exception:
                self._conn.rollback()
                raise
            finally:
                cursor.close()

    def _init_schema(self) -> None:
        """Initialize the database schema with inodes and chunks."""
        # Note: _init_schema runs during __init__ before the lock is needed
        # (no concurrent access possible yet), so we use direct cursor access.
        cursor = self._conn.cursor()
        try:
            # Metadata table
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS inodes (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    path TEXT UNIQUE NOT NULL,
                    is_directory INTEGER NOT NULL DEFAULT 0,
                    created_at TEXT NOT NULL,
                    modified_at TEXT NOT NULL,
                    size INTEGER NOT NULL DEFAULT 0
                )
            ''')
            cursor.execute('CREATE INDEX IF NOT EXISTS idx_inode_path ON inodes(path)')
            
            # Data chunks table
            cursor.execute('''
                CREATE TABLE IF NOT EXISTS chunks (
                    inode_id INTEGER NOT NULL,
                    chunk_index INTEGER NOT NULL,
                    data BLOB,
                    PRIMARY KEY (inode_id, chunk_index),
                    FOREIGN KEY(inode_id) REFERENCES inodes(id) ON DELETE CASCADE
                )
            ''')
            
            # Create root directory
            cursor.execute('SELECT id FROM inodes WHERE path = ?', ('/',))
            if cursor.fetchone() is None:
                now = datetime.now().isoformat()
                cursor.execute(
                    'INSERT INTO inodes (path, is_directory, created_at, modified_at, size) VALUES (?, 1, ?, ?, 0)',
                    ('/', now, now)
                )

            self._conn.commit()
        finally:
            cursor.close()

    # ========================================================================
    # Underlay/Layer Management (for mod/DLC support)
    # ========================================================================

    @require(lambda vfs: vfs is not None, "VFS instance required")
    @require(lambda priority: isinstance(priority, int), "Priority must be an integer")
    @ensure(lambda result: result is not None and len(result) > 0, "Must return a valid name")
    def add_underlay(
        self, 
        vfs: 'VirtualFileSystem', 
        priority: int = 0, 
        name: Optional[str] = None
    ) -> str:
        """
        Add an underlay (fallback layer) for reading files.
        
        When a file is not found locally, underlays are checked in priority order
        (higher priority first). Writes always go to the local (top) layer.
        
        Args:
            vfs: Another VirtualFileSystem instance to use as fallback
            priority: Higher priority underlays are checked first (default: 0)
            name: Human-readable name for this layer (auto-generated if not provided)
        
        Returns:
            The name of the underlay (useful for later removal)
        
        Example:
            # Create layered game filesystem
            game = VirtualFileSystem("user_data.db")
            game.add_underlay(VirtualFileSystem("dlc.db"), priority=10, name="dlc")
            game.add_underlay(VirtualFileSystem("base.db"), priority=0, name="base")
            
            # Reading checks: local -> dlc -> base
            # Writing goes to: local (user_data.db)
        """
        with self._lock:
            if name is None:
                name = f"underlay_{len(self._underlays)}"
            
            if name in self._underlay_by_name:
                raise ValueError(f"Underlay with name '{name}' already exists")
            
            self._underlays.append((priority, name, vfs))
            self._underlays.sort(key=lambda x: -x[0])  # Higher priority first
            self._underlay_by_name[name] = vfs
            
            return name

    @require(lambda name: name is not None and len(name) > 0, "Name must not be empty")
    def remove_underlay(self, name: str) -> bool:
        """
        Remove an underlay by name.
        
        Returns True if the underlay was found and removed.
        """
        with self._lock:
            if name not in self._underlay_by_name:
                return False
            
            vfs = self._underlay_by_name.pop(name)
            self._underlays = [(p, n, v) for p, n, v in self._underlays if n != name]
            return True

    @ensure(lambda result: len(result) >= 1, "Must return at least the local layer")
    @ensure(lambda result: result[0]['name'] == 'local', "First layer must be 'local'")
    def list_layers(self) -> List[dict]:
        """
        List all layers including self (local) and underlays.
        
        Returns list of dicts with layer info, in priority order (local first).
        """
        with self._lock:
            layers = [{
                'name': 'local',
                'priority': float('inf'),  # Local is always highest
                'db_path': self._db_path,
                'writable': not self._read_only,
            }]
            
            for priority, name, vfs in self._underlays:
                layers.append({
                    'name': name,
                    'priority': priority,
                    'db_path': vfs._db_path,
                    'writable': False,
                })
            
            return layers

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    def which_layer(self, path: str) -> Optional[str]:
        """
        Debug helper: Returns which layer would serve this file.
        
        Returns 'local' if in this VFS, underlay name if in an underlay,
        or None if not found in any layer.
        """
        path = self._normalize_path(path)
        
        # Check local first
        if self._exists_local(path):
            return 'local'
        
        # Check underlays
        for _, name, vfs in self._underlays:
            if vfs.exists(path):
                return name
        
        return None

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @ensure(lambda result: 'path' in result and 'resolved_from' in result and 'layers' in result, "Must return valid info dict")
    def which_layer_detailed(self, path: str) -> dict:
        """
        Debug helper: Returns detailed resolution info for a path.
        
        Shows which layers have the file and which one wins.
        """
        path = self._normalize_path(path)
        layers = []
        winner = None
        
        # Check local
        local_exists = self._exists_local(path)
        layers.append({
            'name': 'local',
            'has_file': local_exists,
        })
        if local_exists and winner is None:
            winner = 'local'
        
        # Check underlays
        for _, name, vfs in self._underlays:
            try:
                exists = vfs.exists(path)
                layers.append({
                    'name': name,
                    'has_file': exists,
                })
                if exists and winner is None:
                    winner = name
            except Exception as e:
                layers.append({
                    'name': name,
                    'error': str(e),
                })
        
        return {
            'path': path,
            'resolved_from': winner,
            'layers': layers,
        }

    def _exists_local(self, path: str) -> bool:
        """Check if path exists in local storage only (not underlays)."""
        path = self._normalize_path(path)
        with self._cursor() as cursor:
            cursor.execute('SELECT 1 FROM inodes WHERE path = ?', (path,))
            return cursor.fetchone() is not None

    def _isfile_local(self, path: str) -> bool:
        """Check if path is a file in local storage only."""
        path = self._normalize_path(path)
        with self._cursor() as cursor:
            cursor.execute('SELECT is_directory FROM inodes WHERE path = ?', (path,))
            row = cursor.fetchone()
            return row is not None and not row['is_directory']

    def _isdir_local(self, path: str) -> bool:
        """Check if path is a directory in local storage only."""
        path = self._normalize_path(path)
        with self._cursor() as cursor:
            cursor.execute('SELECT is_directory FROM inodes WHERE path = ?', (path,))
            row = cursor.fetchone()
            return row is not None and row['is_directory']

    def _listdir_local(self, path: str) -> List[str]:
        """List directory contents from local storage only."""
        path = self._normalize_path(path)
        with self._cursor() as cursor:
            search_pattern = path + '/*' if path != '/' else '/*'
            cursor.execute('SELECT path FROM inodes WHERE path GLOB ?', (search_pattern,))
            
            results = set()
            prefix_len = len(path) if path == '/' else len(path) + 1
            
            for row in cursor.fetchall():
                child_path = row['path']
                rel = child_path[prefix_len:]
                if '/' not in rel and rel:
                    results.add(rel)
                    
        return list(results)

    @require(lambda path: path is not None, "Path must not be None")
    @ensure(lambda result: result.startswith('/'), "Normalized path must start with /")
    @ensure(lambda result: '//' not in result, "Normalized path must not have double slashes")
    def _normalize_path(self, path: str) -> str:
        """Normalize a path to absolute POSIX format."""
        if not path.startswith('/'):
            path = '/' + path
        path = posixpath.normpath(path)
        return path

    def _get_parent_path(self, path: str) -> str:
        parent = posixpath.dirname(path)
        return parent if parent else '/'

    def _ensure_parent_exists(self, path: str) -> None:
        """Ensure all parent directories exist.
        
        Iterative implementation to avoid recursion limit on deep paths.
        Thread-safe: handles race conditions where another thread may create
        the directory between our check and our mkdir call.
        """
        parts_to_create = []
        current = self._get_parent_path(path)
        
        # Walk up the tree to find existing ancestor
        while current != '/':
            with self._cursor() as cursor:
                cursor.execute('SELECT id, is_directory FROM inodes WHERE path = ?', (current,))
                row = cursor.fetchone()
            
            if row is not None:
                if not row['is_directory']:
                    raise NotADirectoryError(f"Parent path is not a directory: {current}")
                break  # Found existing directory ancestor
            
            parts_to_create.append(current)
            current = self._get_parent_path(current)
        
        # Create directories in reverse order (parents first)
        for dir_path in reversed(parts_to_create):
            try:
                self.mkdir(dir_path)
            except FileExistsError:
                # Race condition: another thread created it
                if not self.isdir(dir_path):
                    raise NotADirectoryError(f"Parent path is not a directory: {dir_path}")

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @require(
        lambda mode: any(m in mode for m in ('r', 'w', 'a', 'x')),
        "Mode must contain 'r', 'w', 'a', or 'x'"
    )
    @ensure(lambda result: result is not None, "Must return a file object")
    def open(self, path: str, mode: str = 'r', buffering: int = -1, 
             encoding: Optional[str] = None, errors: Optional[str] = None, 
             newline: Optional[str] = None) -> Union[io.BufferedReader, io.BufferedWriter, io.BufferedRandom, io.TextIOWrapper]:
        """
        Open a file and return a standard file-like object.
        
        For read modes: Opens from local if exists, otherwise from highest priority underlay.
        For write modes: Always writes to local (top layer).
        
        Args:
            path: File path
            mode: 'r', 'w', 'a', 'x', 'rb', 'wb', 'ab', 'xb', etc. ('x' = exclusive create)
            buffering: Buffer size. -1 default, 0 for binary unbuffered.
            encoding: Text encoding (e.g. 'utf-8').
            errors: Error handling for encoding.
            newline: Newline translation.
        
        Returns:
            A file object compatible with Python's io module.
        """
        path = self._normalize_path(path)
        
        # Check permissions/existence logic
        writing = 'w' in mode or 'a' in mode or 'x' in mode or '+' in mode
        
        if writing:
            if self._read_only:
                raise PermissionError(f"VFS is read-only: {path}")
            # Writes always go to local
            self._ensure_parent_exists(path)
        elif 'r' in mode:
            # For reading, check if file exists locally first, then underlays
            if self._exists_local(path):
                if self._isdir_local(path):
                    raise IsADirectoryError(f"Is a directory: {path}")
                # File exists locally, will use local VirtualFileRaw below
            else:
                # Check underlays for the file
                for _, name, vfs in self._underlays:
                    if vfs.exists(path):
                        if vfs.isdir(path):
                            raise IsADirectoryError(f"Is a directory: {path}")
                        # Delegate to underlay's open method
                        return vfs.open(path, mode, buffering, encoding, errors, newline)
                
                # Not found anywhere
                raise FileNotFoundError(f"File not found: {path}")

        # Create raw stream (for local access)
        raw = VirtualFileRaw(self, path, mode)
        
        # If buffering is explicitly 0, return raw (only valid for binary)
        if buffering == 0:
            if 'b' not in mode:
                raise ValueError("can't have unbuffered text I/O")
            return raw

        # Wrap in buffer
        line_buffering = (buffering == 1)
        buffer_size = buffering if buffering > 1 else io.DEFAULT_BUFFER_SIZE
        
        if '+' in mode:
            buffer = io.BufferedRandom(raw, buffer_size)
        elif writing:
            buffer = io.BufferedWriter(raw, buffer_size)
        else:
            buffer = io.BufferedReader(raw, buffer_size)

        if 'b' in mode:
            return buffer
        
        # Text wrapper
        return io.TextIOWrapper(buffer, encoding=encoding, errors=errors, newline=newline, line_buffering=line_buffering)

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @require(lambda path: path != '/', "Cannot create root directory")
    def mkdir(self, path: str, parents: bool = False, exist_ok: bool = False) -> None:
        path = self._normalize_path(path)
        
        with self._cursor() as cursor:
            cursor.execute('SELECT id, is_directory FROM inodes WHERE path = ?', (path,))
            row = cursor.fetchone()

            if row is not None:
                if row['is_directory']:
                    if exist_ok:
                        return
                    raise FileExistsError(f"Directory already exists: {path}")
                else:
                    raise FileExistsError(f"File exists: {path}")

            if parents:
                # Release lock before recursive call to avoid deadlock
                pass
            else:
                parent = self._get_parent_path(path)
                cursor.execute('SELECT is_directory FROM inodes WHERE path = ?', (parent,))
                parent_row = cursor.fetchone()
                if parent_row is None:
                    raise FileNotFoundError(f"Parent directory not found: {parent}")
                if not parent_row['is_directory']:
                    raise NotADirectoryError(f"Parent is not a directory: {parent}")

        if parents:
            self._ensure_parent_exists(path)

        try:
            with self._transaction() as cursor:
                now = datetime.now().isoformat()
                cursor.execute(
                    'INSERT INTO inodes (path, is_directory, created_at, modified_at, size) VALUES (?, 1, ?, ?, 0)',
                    (path, now, now)
                )
        except sqlite3.IntegrityError:
            # Race condition: another thread created this path between our check and INSERT
            # Re-check what exists now
            if self._isdir_local(path):
                if exist_ok:
                    return
                raise FileExistsError(f"Directory already exists: {path}")
            elif self._isfile_local(path):
                raise FileExistsError(f"File exists: {path}")
            else:
                # Something unexpected - re-raise original
                raise

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    def makedirs(self, path: str, exist_ok: bool = False) -> None:
        self.mkdir(path, parents=True, exist_ok=exist_ok)

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @require(lambda path: path != '/', "Cannot remove root directory")
    def rmdir(self, path: str) -> None:
        path = self._normalize_path(path)
        if path == '/':
            raise PermissionError("Cannot remove root directory")

        with self._transaction() as cursor:
            cursor.execute('SELECT id, is_directory FROM inodes WHERE path = ?', (path,))
            row = cursor.fetchone()

            if row is None:
                raise FileNotFoundError(f"Directory not found: {path}")
            if not row['is_directory']:
                raise NotADirectoryError(f"Not a directory: {path}")

            # Check if empty (look for any child paths)
            cursor.execute('SELECT id FROM inodes WHERE path GLOB ?', (path + '/*',))
            if cursor.fetchone() is not None:
                raise PermissionError(f"Directory not empty: {path}")

            cursor.execute('DELETE FROM inodes WHERE id = ?', (row['id'],))

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    def remove(self, path: str) -> None:
        path = self._normalize_path(path)
        
        with self._transaction() as cursor:
            cursor.execute('SELECT id, is_directory FROM inodes WHERE path = ?', (path,))
            row = cursor.fetchone()

            if row is None:
                raise FileNotFoundError(f"File not found: {path}")
            if row['is_directory']:
                raise IsADirectoryError(f"Is a directory: {path}")

            # Cascade delete will remove chunks due to PRAGMA foreign_keys = ON
            cursor.execute('DELETE FROM inodes WHERE id = ?', (row['id'],))

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    def unlink(self, path: str) -> None:
        self.remove(path)

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @require(lambda path: path != '/', "Cannot remove root directory")
    def rmtree(self, path: str) -> None:
        path = self._normalize_path(path)
        if path == '/':
            raise PermissionError("Cannot remove root directory")

        with self._transaction() as cursor:
            cursor.execute('SELECT id FROM inodes WHERE path = ?', (path,))
            if cursor.fetchone() is None:
                raise FileNotFoundError(f"Path not found: {path}")

            # Delete path and all children using GLOB
            # Logic: DELETE FROM inodes WHERE path = target OR path GLOB target/*
            cursor.execute('DELETE FROM inodes WHERE path = ? OR path GLOB ?', (path, path + '/*'))

    @require(lambda src: src is not None and len(src) > 0, "Source path must not be empty")
    @require(lambda dst: dst is not None and len(dst) > 0, "Destination path must not be empty")
    @require(lambda src: src != '/', "Cannot rename root directory")
    def rename(self, src: str, dst: str) -> None:
        src = self._normalize_path(src)
        dst = self._normalize_path(dst)

        # First, check source and ensure parent exists (outside transaction to avoid lock issues)
        with self._cursor() as cursor:
            cursor.execute('SELECT id, is_directory FROM inodes WHERE path = ?', (src,))
            row = cursor.fetchone()

            if row is None:
                raise FileNotFoundError(f"Source not found: {src}")

            cursor.execute('SELECT id FROM inodes WHERE path = ?', (dst,))
            if cursor.fetchone() is not None:
                raise FileExistsError(f"Destination exists: {dst}")
            
            is_directory = row['is_directory']

        self._ensure_parent_exists(dst)
        
        # Perform atomic rename within a transaction
        with self._transaction() as cursor:
            now = datetime.now().isoformat()

            if is_directory:
                # Rename directory and all children recursively
                # Find all children
                cursor.execute('SELECT path FROM inodes WHERE path GLOB ?', (src + '/*',))
                children = [child['path'] for child in cursor.fetchall()]
                
                # Update children
                for child_path in children:
                    suffix = child_path[len(src):]
                    new_child_path = dst + suffix
                    cursor.execute('UPDATE inodes SET path = ?, modified_at = ? WHERE path = ?', 
                                   (new_child_path, now, child_path))
            
            # Update self
            cursor.execute('UPDATE inodes SET path = ?, modified_at = ? WHERE path = ?', 
                           (dst, now, src))

    @require(lambda src: src is not None and len(src) > 0, "Source path must not be empty")
    @require(lambda dst: dst is not None and len(dst) > 0, "Destination path must not be empty")
    def copy(self, src: str, dst: str) -> None:
        """Copy a file using stream IO."""
        # Note: We could use SQL-level copying for speed, but stream IO ensures 
        # we invoke the standard write logic (stat updates, etc) correctly.
        # For a "Big Data" safe copy, we chunk the copy.
        if self.isdir(src):
            raise IsADirectoryError(f"Is a directory: {src}")
            
        with self.open(src, 'rb') as fsrc, self.open(dst, 'wb') as fdst:
            while True:
                buf = fsrc.read(1024 * 1024) # 1MB copy buffer
                if not buf:
                    break
                fdst.write(buf)

    @require(lambda src: src is not None and len(src) > 0, "Source path must not be empty")
    @require(lambda dst: dst is not None and len(dst) > 0, "Destination path must not be empty")
    def copytree(self, src: str, dst: str) -> None:
        src = self._normalize_path(src)
        dst = self._normalize_path(dst)

        # Check existence FIRST before checking isdir
        if not self.exists(src):
            raise FileNotFoundError(f"Source not found: {src}")

        if not self.isdir(src):
            raise NotADirectoryError(f"Not a directory: {src}")
            
        if self.exists(dst):
            raise FileExistsError(f"Destination exists: {dst}")

        self.makedirs(dst)

        for dirpath, dirnames, filenames in self.walk(src):
            rel_path = dirpath[len(src):].lstrip('/')
            dst_dir = posixpath.join(dst, rel_path) if rel_path else dst

            for dirname in dirnames:
                self.mkdir(posixpath.join(dst_dir, dirname), exist_ok=True)

            for filename in filenames:
                src_file = posixpath.join(dirpath, filename)
                dst_file = posixpath.join(dst_dir, filename)
                self.copy(src_file, dst_file)

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    def exists(self, path: str) -> bool:
        """Check if path exists (local or any underlay)."""
        path = self._normalize_path(path)
        
        # Check local first
        if self._exists_local(path):
            return True
        
        # Check underlays
        for _, _, vfs in self._underlays:
            if vfs.exists(path):
                return True
        
        return False

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    def isfile(self, path: str) -> bool:
        """Check if path is a file (local or any underlay)."""
        path = self._normalize_path(path)
        
        # Check local first
        if self._exists_local(path):
            return self._isfile_local(path)
        
        # Check underlays
        for _, _, vfs in self._underlays:
            if vfs.exists(path):
                return vfs.isfile(path)
        
        return False

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    def isdir(self, path: str) -> bool:
        """Check if path is a directory (local or any underlay)."""
        path = self._normalize_path(path)
        
        # Check local first
        if self._exists_local(path):
            return self._isdir_local(path)
        
        # Check underlays - a dir exists if ANY layer has it as a dir
        for _, _, vfs in self._underlays:
            if vfs.isdir(path):
                return True
        
        return False

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @ensure(lambda result: isinstance(result, list), "Must return a list")
    @ensure(lambda result: all(isinstance(item, str) for item in result), "All items must be strings")
    def listdir(self, path: str = '/') -> List[str]:
        """List directory contents merged from local and all underlays."""
        path = self._normalize_path(path)
        
        # Check if it's a directory in any layer
        if not self.isdir(path):
            if self.exists(path):
                raise NotADirectoryError(f"Not a directory: {path}")
            raise FileNotFoundError(f"Directory not found: {path}")

        # Merge results from all layers
        results = set()
        
        # Local entries
        if self._isdir_local(path):
            results.update(self._listdir_local(path))
        
        # Underlay entries
        for _, _, vfs in self._underlays:
            try:
                if vfs.isdir(path):
                    results.update(vfs.listdir(path))
            except (FileNotFoundError, NotADirectoryError):
                continue
                    
        return sorted(list(results))

    @require(lambda top: top is not None and len(top) > 0, "Top path must not be empty")
    def walk(
        self, 
        top: str = '/', 
        onerror: Optional[Callable[[OSError], None]] = None
    ) -> Iterator[Tuple[str, List[str], List[str]]]:
        """Walk the directory tree, yielding (dirpath, dirnames, filenames) tuples.
        
        Args:
            top: Starting directory path
            onerror: Optional callback for errors (like os.walk). If not provided,
                     errors are silently ignored.
        """
        top = self._normalize_path(top)
        
        try:
            entries = self.listdir(top)
        except OSError as e:
            if onerror is not None:
                onerror(e)
            return

        dirs = []
        files = []

        for entry in entries:
            entry_path = posixpath.join(top, entry)
            if self.isdir(entry_path):
                dirs.append(entry)
            else:
                files.append(entry)

        yield top, dirs, files

        for d in dirs:
            yield from self.walk(posixpath.join(top, d))

    @require(lambda pattern: pattern is not None and len(pattern) > 0, "Pattern must not be empty")
    @ensure(lambda result: isinstance(result, list), "Must return a list")
    def glob(self, pattern: str) -> List[str]:
        """
        Find files matching the pattern using SQLite GLOB syntax.
        Results merged from local and all underlays.
        
        Note: Uses SQLite GLOB, not Python fnmatch. Key differences:
          - Character class negation: [^abc] not [!abc]
          - Case-sensitive by default
          - * matches any sequence, ? matches single char
        
        Example:
            vfs.glob("/*.txt")           # All .txt files in root
            vfs.glob("/images/img[0-9].png")  # img0.png through img9.png
            vfs.glob("/data/[^.]*.json")     # JSON files not starting with .
        """
        pattern = self._normalize_path(pattern)
        results = set()
        
        # Local matches
        with self._cursor() as cursor:
            cursor.execute('SELECT path FROM inodes WHERE path GLOB ?', (pattern,))
            for row in cursor.fetchall():
                results.add(row['path'])
        
        # Underlay matches
        for _, _, vfs in self._underlays:
            try:
                for path in vfs.glob(pattern):
                    results.add(path)
            except Exception:
                continue
        
        return sorted(list(results))

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @ensure(lambda result: result.st_size >= 0, "Size must be non-negative")
    def stat(self, path: str) -> StatResult:
        """Get file stats from local or underlay.
        
        Returns a StatResult namedtuple compatible with os.stat() interface.
        Access via st_size, st_ctime, st_mtime, st_mode, or convenience properties.
        """
        path = self._normalize_path(path)
        
        # Check local first
        with self._cursor() as cursor:
            cursor.execute(
                'SELECT size, created_at, modified_at, is_directory FROM inodes WHERE path = ?',
                (path,)
            )
            row = cursor.fetchone()

            if row is not None:
                is_dir = bool(row['is_directory'])
                mode = stat_module.S_IFDIR | 0o755 if is_dir else stat_module.S_IFREG | 0o644
                return StatResult(
                    st_size=row['size'],
                    st_ctime=row['created_at'],
                    st_mtime=row['modified_at'],
                    st_mode=mode,
                    source_layer='local',
                )
        
        # Check underlays
        for _, name, vfs in self._underlays:
            try:
                if vfs.exists(path):
                    result = vfs.stat(path)
                    # Return new StatResult with updated source_layer
                    return StatResult(
                        st_size=result.st_size,
                        st_ctime=result.st_ctime,
                        st_mtime=result.st_mtime,
                        st_mode=result.st_mode,
                        source_layer=name,
                    )
            except FileNotFoundError:
                continue
        
        raise FileNotFoundError(f"Path not found: {path}")

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @ensure(lambda result: result >= 0, "Size must be non-negative")
    def getsize(self, path: str) -> int:
        return self.stat(path).st_size

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @ensure(lambda result: isinstance(result, str), "Must return a string")
    def read_text(self, path: str, encoding: str = 'utf-8') -> str:
        """Helper to read full file as text."""
        with self.open(path, 'r', encoding=encoding) as f:
            return f.read()

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @require(lambda content: content is not None, "Content must not be None")
    @ensure(lambda result: result >= 0, "Bytes written must be non-negative")
    def write_text(self, path: str, content: str, encoding: str = 'utf-8') -> int:
        """Helper to write text to file."""
        with self.open(path, 'w', encoding=encoding) as f:
            return f.write(content)

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @ensure(lambda result: isinstance(result, bytes), "Must return bytes")
    def read_bytes(self, path: str) -> bytes:
        """Helper to read full file as bytes."""
        with self.open(path, 'rb') as f:
            return f.read()

    @require(lambda path: path is not None and len(path) > 0, "Path must not be empty")
    @require(lambda content: content is not None, "Content must not be None")
    @ensure(lambda result: result >= 0, "Bytes written must be non-negative")
    def write_bytes(self, path: str, content: bytes) -> int:
        """Helper to write bytes to file."""
        with self.open(path, 'wb') as f:
            return f.write(content)

    def flush(self) -> None:
        """Flush pending writes to database."""
        with self._lock:
            self._conn.commit()
    
    def sync(self) -> None:
        """Ensure all data is durably written to disk.
        
        Calls commit and WAL checkpoint for maximum durability.
        Useful for critical saves like game save files.
        """
        with self._lock:
            self._conn.commit()
            # Force WAL checkpoint if using WAL mode
            try:
                self._conn.execute("PRAGMA wal_checkpoint(FULL)")
            except sqlite3.OperationalError:
                pass  # Not in WAL mode, ignore
    
    @contextmanager
    def atomic_batch(self):
        """Context manager for atomic multi-file operations.
        
        Groups multiple operations into a single transaction.
        All succeed or all fail together.
        
        Example:
            with vfs.atomic_batch():
                vfs.write_text("/save/player.json", player_data)
                vfs.write_text("/save/world.json", world_data)
                vfs.write_text("/save/meta.json", meta_data)
        """
        with self._lock:
            try:
                yield
                self._conn.commit()
            except Exception:
                self._conn.rollback()
                raise

    def close(self) -> None:
        """Close the database connection."""
        if self._conn:
            try:
                self._conn.close()
            except Exception:
                pass
            self._conn = None
            self._closed = True
    
    def __del__(self):
        """Ensure connection is closed on garbage collection."""
        try:
            self.close()
        except Exception:
            pass  # Avoid exceptions in __del__

    def __enter__(self) -> 'VirtualFileSystem':
        return self

    def __exit__(self, exc_type, exc_val, exc_tb) -> None:
        self.close()