"""Virtual File System library backed by SQLite3."""

from .vfs import (
    VirtualFileSystem,
    VirtualFileSystemError,
    FileNotFoundError,
    FileExistsError,
    IsADirectoryError,
    NotADirectoryError,
    PermissionError,
)

__all__ = [
    'VirtualFileSystem',
    'VirtualFileSystemError',
    'FileNotFoundError',
    'FileExistsError',
    'IsADirectoryError',
    'NotADirectoryError',
    'PermissionError',
]
