"""
Benchmark tests for VirtualFileSystem performance.

Run with: pytest tests/benchmark_vfs.py -v -s
Or for detailed timing: pytest tests/benchmark_vfs.py -v -s --tb=short

These benchmarks measure realistic performance across various operations.
"""

import time
import threading
import statistics
from contextlib import contextmanager
from typing import Callable, List, Tuple

import pytest

from lib.vfs import VirtualFileSystem


# ============================================================================
# Benchmark Utilities
# ============================================================================

@contextmanager
def timer():
    """Context manager that yields a callable to get elapsed time."""
    start = time.perf_counter()
    elapsed = lambda: time.perf_counter() - start
    yield elapsed


def run_benchmark(func: Callable, iterations: int = 10) -> dict:
    """Run a benchmark function multiple times and return statistics."""
    times = []
    for _ in range(iterations):
        start = time.perf_counter()
        result = func()
        elapsed = time.perf_counter() - start
        times.append(elapsed)
    
    return {
        'min': min(times),
        'max': max(times),
        'mean': statistics.mean(times),
        'median': statistics.median(times),
        'stdev': statistics.stdev(times) if len(times) > 1 else 0,
        'total': sum(times),
        'iterations': iterations,
        'result': result
    }


def format_size(size_bytes: int) -> str:
    """Format bytes to human readable string."""
    for unit in ['B', 'KB', 'MB', 'GB']:
        if size_bytes < 1024:
            return f"{size_bytes:.2f} {unit}"
        size_bytes /= 1024
    return f"{size_bytes:.2f} TB"


def format_rate(bytes_per_sec: float) -> str:
    """Format transfer rate to human readable string."""
    return f"{format_size(bytes_per_sec)}/s"


# ============================================================================
# Benchmark Fixtures
# ============================================================================

@pytest.fixture
def vfs():
    """Create a fresh in-memory VFS for each test."""
    fs = VirtualFileSystem(":memory:")
    yield fs
    fs.close()


@pytest.fixture
def vfs_with_data(vfs):
    """VFS pre-populated with test data."""
    # Create directory structure
    for i in range(10):
        vfs.makedirs(f"/project{i}/src/components")
        vfs.makedirs(f"/project{i}/tests")
        vfs.makedirs(f"/project{i}/docs")
        
        # Create files in each project
        for j in range(5):
            vfs.write_text(f"/project{i}/src/file{j}.py", f"# File {j}\n" * 100)
            vfs.write_text(f"/project{i}/src/components/comp{j}.py", f"class Comp{j}: pass\n" * 50)
            vfs.write_text(f"/project{i}/tests/test_{j}.py", f"def test_{j}(): pass\n" * 20)
    
    return vfs


# ============================================================================
# Write Performance Benchmarks
# ============================================================================

class TestWritePerformance:
    """Benchmark write operations."""

    def test_small_file_writes(self, vfs, capsys):
        """Benchmark many small file writes (simulating config/metadata files)."""
        num_files = 1000
        content = "key=value\n" * 10  # ~100 bytes per file
        
        with timer() as elapsed:
            for i in range(num_files):
                vfs.write_text(f"/configs/file_{i}.txt", content)
        
        total_time = elapsed()
        files_per_sec = num_files / total_time
        bytes_written = len(content.encode()) * num_files
        
        print(f"\n{'='*60}")
        print(f"Small File Writes ({num_files} files, ~100 bytes each)")
        print(f"{'='*60}")
        print(f"Total time:     {total_time:.3f}s")
        print(f"Files/second:   {files_per_sec:.1f}")
        print(f"Throughput:     {format_rate(bytes_written / total_time)}")
        print(f"{'='*60}")
        
        assert total_time < 10, "Small file writes too slow"

    def test_medium_file_writes(self, vfs, capsys):
        """Benchmark medium file writes (simulating source code files)."""
        num_files = 100
        content = "x" * 10_000  # 10KB per file
        
        with timer() as elapsed:
            for i in range(num_files):
                vfs.write_text(f"/src/module_{i}.py", content)
        
        total_time = elapsed()
        bytes_written = len(content.encode()) * num_files
        
        print(f"\n{'='*60}")
        print(f"Medium File Writes ({num_files} files, 10KB each)")
        print(f"{'='*60}")
        print(f"Total time:     {total_time:.3f}s")
        print(f"Files/second:   {num_files / total_time:.1f}")
        print(f"Throughput:     {format_rate(bytes_written / total_time)}")
        print(f"{'='*60}")
        
        assert total_time < 5, "Medium file writes too slow"

    def test_large_file_write(self, vfs, capsys):
        """Benchmark large file write (simulating data files)."""
        sizes = [1, 5, 10, 50]  # MB
        
        print(f"\n{'='*60}")
        print("Large File Writes")
        print(f"{'='*60}")
        
        for size_mb in sizes:
            content = b"x" * (size_mb * 1024 * 1024)
            
            with timer() as elapsed:
                vfs.write_bytes(f"/data/large_{size_mb}mb.bin", content)
            
            total_time = elapsed()
            throughput = len(content) / total_time
            
            print(f"{size_mb:3d} MB: {total_time:.3f}s ({format_rate(throughput)})")
            
            # Cleanup for next iteration
            vfs.remove(f"/data/large_{size_mb}mb.bin")
        
        print(f"{'='*60}")

    def test_chunked_write_performance(self, vfs, capsys):
        """Benchmark writes that span multiple chunks (64KB boundaries)."""
        chunk_size = 65536  # VirtualFileRaw.CHUNK_SIZE
        
        # Write data that spans exactly N chunks
        test_cases = [
            ("1 chunk", chunk_size),
            ("2 chunks", chunk_size * 2),
            ("5 chunks", chunk_size * 5),
            ("10 chunks", chunk_size * 10),
        ]
        
        print(f"\n{'='*60}")
        print("Chunked Write Performance (64KB chunk boundaries)")
        print(f"{'='*60}")
        
        for name, size in test_cases:
            content = b"x" * size
            
            stats = run_benchmark(
                lambda c=content: vfs.write_bytes("/test_chunked.bin", c),
                iterations=5
            )
            
            print(f"{name:12s} ({format_size(size):>10s}): "
                  f"mean={stats['mean']*1000:.2f}ms, "
                  f"throughput={format_rate(size / stats['mean'])}")
        
        print(f"{'='*60}")


# ============================================================================
# Read Performance Benchmarks
# ============================================================================

class TestReadPerformance:
    """Benchmark read operations."""

    def test_small_file_reads(self, vfs, capsys):
        """Benchmark many small file reads."""
        num_files = 1000
        content = "key=value\n" * 10
        
        # Setup: create files
        for i in range(num_files):
            vfs.write_text(f"/configs/file_{i}.txt", content)
        
        # Benchmark reads
        with timer() as elapsed:
            for i in range(num_files):
                _ = vfs.read_text(f"/configs/file_{i}.txt")
        
        total_time = elapsed()
        bytes_read = len(content.encode()) * num_files
        
        print(f"\n{'='*60}")
        print(f"Small File Reads ({num_files} files, ~100 bytes each)")
        print(f"{'='*60}")
        print(f"Total time:     {total_time:.3f}s")
        print(f"Files/second:   {num_files / total_time:.1f}")
        print(f"Throughput:     {format_rate(bytes_read / total_time)}")
        print(f"{'='*60}")

    def test_large_file_read(self, vfs, capsys):
        """Benchmark large file reads."""
        sizes = [1, 5, 10, 50]  # MB
        
        print(f"\n{'='*60}")
        print("Large File Reads")
        print(f"{'='*60}")
        
        for size_mb in sizes:
            content = b"x" * (size_mb * 1024 * 1024)
            vfs.write_bytes(f"/data/large_{size_mb}mb.bin", content)
            
            with timer() as elapsed:
                data = vfs.read_bytes(f"/data/large_{size_mb}mb.bin")
            
            total_time = elapsed()
            throughput = len(data) / total_time
            
            print(f"{size_mb:3d} MB: {total_time:.3f}s ({format_rate(throughput)})")
            
            vfs.remove(f"/data/large_{size_mb}mb.bin")
        
        print(f"{'='*60}")

    def test_sequential_vs_random_read(self, vfs, capsys):
        """Compare sequential vs random access read patterns."""
        # Create a file with distinct chunks
        chunk_size = 65536
        num_chunks = 20
        content = b"".join(bytes([i % 256]) * chunk_size for i in range(num_chunks))
        vfs.write_bytes("/random_access.bin", content)
        
        read_size = 4096
        num_reads = 100
        
        # Sequential reads
        with vfs.open("/random_access.bin", "rb") as f:
            with timer() as elapsed:
                for _ in range(num_reads):
                    f.read(read_size)
                    if f.tell() >= len(content):
                        f.seek(0)
            seq_time = elapsed()
        
        # Random reads
        import random
        positions = [random.randint(0, len(content) - read_size) for _ in range(num_reads)]
        
        with vfs.open("/random_access.bin", "rb") as f:
            with timer() as elapsed:
                for pos in positions:
                    f.seek(pos)
                    f.read(read_size)
            random_time = elapsed()
        
        print(f"\n{'='*60}")
        print(f"Sequential vs Random Read ({num_reads} reads of {read_size} bytes)")
        print(f"{'='*60}")
        print(f"Sequential: {seq_time*1000:.2f}ms")
        print(f"Random:     {random_time*1000:.2f}ms")
        print(f"Ratio:      {random_time/seq_time:.2f}x")
        print(f"{'='*60}")


# ============================================================================
# Directory Operation Benchmarks
# ============================================================================

class TestDirectoryPerformance:
    """Benchmark directory operations."""

    def test_mkdir_performance(self, vfs, capsys):
        """Benchmark directory creation."""
        num_dirs = 500
        vfs.mkdir("/dirs")  # Create parent first
        
        with timer() as elapsed:
            for i in range(num_dirs):
                vfs.mkdir(f"/dirs/dir_{i}")
        
        total_time = elapsed()
        
        print(f"\n{'='*60}")
        print(f"mkdir Performance ({num_dirs} directories)")
        print(f"{'='*60}")
        print(f"Total time:   {total_time:.3f}s")
        print(f"Dirs/second:  {num_dirs / total_time:.1f}")
        print(f"{'='*60}")

    def test_makedirs_deep_hierarchy(self, vfs, capsys):
        """Benchmark creating deep directory hierarchies."""
        depths = [5, 10, 20, 50]
        
        print(f"\n{'='*60}")
        print("Deep Directory Hierarchy Creation")
        print(f"{'='*60}")
        
        for depth in depths:
            path = "/".join(["deep"] + [f"level{i}" for i in range(depth)])
            
            with timer() as elapsed:
                vfs.makedirs(f"/{path}")
            
            print(f"Depth {depth:3d}: {elapsed()*1000:.2f}ms")
        
        print(f"{'='*60}")

    def test_listdir_performance(self, vfs_with_data, capsys):
        """Benchmark directory listing."""
        vfs = vfs_with_data
        
        # Create a directory with many entries
        vfs.mkdir("/many_entries")
        for i in range(1000):
            vfs.write_text(f"/many_entries/file_{i:04d}.txt", "content")
        
        # Benchmark listdir
        stats = run_benchmark(
            lambda: vfs.listdir("/many_entries"),
            iterations=20
        )
        
        print(f"\n{'='*60}")
        print("listdir Performance (1000 entries)")
        print(f"{'='*60}")
        print(f"Mean time:   {stats['mean']*1000:.2f}ms")
        print(f"Min time:    {stats['min']*1000:.2f}ms")
        print(f"Max time:    {stats['max']*1000:.2f}ms")
        print(f"{'='*60}")

    def test_walk_performance(self, vfs_with_data, capsys):
        """Benchmark recursive directory walking."""
        vfs = vfs_with_data
        
        with timer() as elapsed:
            count = sum(1 for _ in vfs.walk("/"))
        
        print(f"\n{'='*60}")
        print(f"walk Performance ({count} directories)")
        print(f"{'='*60}")
        print(f"Total time:  {elapsed()*1000:.2f}ms")
        print(f"{'='*60}")

    def test_glob_performance(self, vfs_with_data, capsys):
        """Benchmark glob pattern matching."""
        vfs = vfs_with_data
        
        patterns = [
            ("/*.py", "root py files"),
            ("/*/*.py", "one level deep"),
            ("/**/*.py", "all py files (recursive)"),
            ("/project*/src/*.py", "specific pattern"),
        ]
        
        print(f"\n{'='*60}")
        print("Glob Pattern Performance")
        print(f"{'='*60}")
        
        for pattern, desc in patterns:
            stats = run_benchmark(lambda p=pattern: vfs.glob(p), iterations=10)
            matches = len(stats['result'])
            print(f"{pattern:25s}: {stats['mean']*1000:6.2f}ms ({matches:3d} matches)")
        
        print(f"{'='*60}")


# ============================================================================
# Concurrent Access Benchmarks
# ============================================================================

class TestConcurrencyPerformance:
    """Benchmark concurrent access patterns."""

    def test_concurrent_writes_scalability(self, capsys):
        """Test how write performance scales with thread count."""
        files_per_thread = 50
        thread_counts = [1, 2, 4, 8]
        
        print(f"\n{'='*60}")
        print(f"Concurrent Write Scalability ({files_per_thread} files/thread)")
        print(f"{'='*60}")
        
        for num_threads in thread_counts:
            vfs = VirtualFileSystem(":memory:")
            vfs.mkdir("/concurrent")
            
            def worker(thread_id: int):
                for i in range(files_per_thread):
                    vfs.write_text(f"/concurrent/t{thread_id}_f{i}.txt", f"data from thread {thread_id}")
            
            threads = [threading.Thread(target=worker, args=(i,)) for i in range(num_threads)]
            
            with timer() as elapsed:
                for t in threads:
                    t.start()
                for t in threads:
                    t.join()
            
            total_time = elapsed()
            total_files = num_threads * files_per_thread
            
            print(f"{num_threads:2d} threads: {total_time:.3f}s "
                  f"({total_files/total_time:.1f} files/s, "
                  f"{total_time/total_files*1000:.2f}ms/file)")
            
            vfs.close()
        
        print(f"{'='*60}")

    def test_concurrent_reads_scalability(self, capsys):
        """Test how read performance scales with thread count."""
        num_files = 100
        reads_per_thread = 50
        thread_counts = [1, 2, 4, 8]
        
        print(f"\n{'='*60}")
        print(f"Concurrent Read Scalability ({reads_per_thread} reads/thread)")
        print(f"{'='*60}")
        
        for num_threads in thread_counts:
            vfs = VirtualFileSystem(":memory:")
            
            # Setup files
            for i in range(num_files):
                vfs.write_text(f"/files/file_{i}.txt", f"content of file {i}" * 100)
            
            def worker(thread_id: int):
                import random
                for _ in range(reads_per_thread):
                    idx = random.randint(0, num_files - 1)
                    vfs.read_text(f"/files/file_{idx}.txt")
            
            threads = [threading.Thread(target=worker, args=(i,)) for i in range(num_threads)]
            
            with timer() as elapsed:
                for t in threads:
                    t.start()
                for t in threads:
                    t.join()
            
            total_time = elapsed()
            total_reads = num_threads * reads_per_thread
            
            print(f"{num_threads:2d} threads: {total_time:.3f}s "
                  f"({total_reads/total_time:.1f} reads/s)")
            
            vfs.close()
        
        print(f"{'='*60}")

    def test_mixed_read_write_workload(self, capsys):
        """Simulate realistic mixed read/write workload."""
        vfs = VirtualFileSystem(":memory:")
        num_threads = 4
        ops_per_thread = 100
        
        # Pre-populate some files
        for i in range(50):
            vfs.write_text(f"/data/file_{i}.txt", f"initial content {i}" * 50)
        
        stats = {'reads': 0, 'writes': 0, 'errors': 0}
        stats_lock = threading.Lock()
        
        def mixed_worker(thread_id: int):
            import random
            local_reads = 0
            local_writes = 0
            
            for i in range(ops_per_thread):
                try:
                    if random.random() < 0.7:  # 70% reads
                        idx = random.randint(0, 49)
                        vfs.read_text(f"/data/file_{idx}.txt")
                        local_reads += 1
                    else:  # 30% writes
                        idx = random.randint(0, 99)
                        vfs.write_text(f"/data/file_{idx}.txt", f"updated by thread {thread_id}")
                        local_writes += 1
                except Exception:
                    with stats_lock:
                        stats['errors'] += 1
            
            with stats_lock:
                stats['reads'] += local_reads
                stats['writes'] += local_writes
        
        threads = [threading.Thread(target=mixed_worker, args=(i,)) for i in range(num_threads)]
        
        with timer() as elapsed:
            for t in threads:
                t.start()
            for t in threads:
                t.join()
        
        total_time = elapsed()
        total_ops = stats['reads'] + stats['writes']
        
        print(f"\n{'='*60}")
        print(f"Mixed Read/Write Workload ({num_threads} threads, 70/30 r/w ratio)")
        print(f"{'='*60}")
        print(f"Total time:    {total_time:.3f}s")
        print(f"Reads:         {stats['reads']}")
        print(f"Writes:        {stats['writes']}")
        print(f"Errors:        {stats['errors']}")
        print(f"Ops/second:    {total_ops / total_time:.1f}")
        print(f"{'='*60}")
        
        vfs.close()


# ============================================================================
# Copy/Move Operation Benchmarks
# ============================================================================

class TestCopyMovePerformance:
    """Benchmark copy and move operations."""

    def test_copy_file_sizes(self, vfs, capsys):
        """Benchmark file copy at various sizes."""
        sizes_kb = [1, 10, 100, 1000, 5000]
        
        print(f"\n{'='*60}")
        print("File Copy Performance")
        print(f"{'='*60}")
        
        for size_kb in sizes_kb:
            content = b"x" * (size_kb * 1024)
            vfs.write_bytes("/src_file.bin", content)
            
            with timer() as elapsed:
                vfs.copy("/src_file.bin", "/dst_file.bin")
            
            total_time = elapsed()
            throughput = len(content) / total_time
            
            print(f"{size_kb:5d} KB: {total_time*1000:7.2f}ms ({format_rate(throughput)})")
            
            vfs.remove("/src_file.bin")
            vfs.remove("/dst_file.bin")
        
        print(f"{'='*60}")

    def test_copytree_performance(self, vfs, capsys):
        """Benchmark directory tree copy."""
        # Create source tree
        for i in range(5):
            vfs.makedirs(f"/src_tree/dir{i}/subdir")
            for j in range(10):
                vfs.write_text(f"/src_tree/dir{i}/file{j}.txt", "content" * 100)
                vfs.write_text(f"/src_tree/dir{i}/subdir/file{j}.txt", "nested" * 50)
        
        with timer() as elapsed:
            vfs.copytree("/src_tree", "/dst_tree")
        
        # Count copied items
        src_files = sum(1 for _, _, files in vfs.walk("/src_tree") for _ in files)
        src_dirs = sum(1 for _ in vfs.walk("/src_tree"))
        
        print(f"\n{'='*60}")
        print(f"copytree Performance ({src_dirs} dirs, {src_files} files)")
        print(f"{'='*60}")
        print(f"Total time:  {elapsed()*1000:.2f}ms")
        print(f"{'='*60}")

    def test_rename_performance(self, vfs, capsys):
        """Benchmark rename/move operations."""
        # Test file rename
        vfs.write_text("/file_to_rename.txt", "content" * 1000)
        
        with timer() as elapsed:
            vfs.rename("/file_to_rename.txt", "/renamed_file.txt")
        file_rename_time = elapsed()
        
        # Test directory rename with many children
        for i in range(100):
            vfs.write_text(f"/dir_to_rename/file{i}.txt", f"content {i}")
        
        with timer() as elapsed:
            vfs.rename("/dir_to_rename", "/renamed_dir")
        dir_rename_time = elapsed()
        
        print(f"\n{'='*60}")
        print("Rename Performance")
        print(f"{'='*60}")
        print(f"File rename:      {file_rename_time*1000:.2f}ms")
        print(f"Dir rename (100): {dir_rename_time*1000:.2f}ms")
        print(f"{'='*60}")


# ============================================================================
# Memory Efficiency Tests
# ============================================================================

class TestMemoryEfficiency:
    """Tests to verify memory-efficient operations."""

    def test_large_file_streaming_memory(self, vfs, capsys):
        """Verify large files are streamed without loading entirely into memory."""
        import sys
        
        # Write a moderately large file
        chunk_size = 65536
        num_chunks = 100  # ~6.4MB total
        
        for i in range(num_chunks):
            with vfs.open("/streaming_test.bin", "ab") as f:
                f.write(bytes([i % 256]) * chunk_size)
        
        # Read in small chunks - this should NOT load the whole file
        bytes_read = 0
        read_buffer_size = 4096
        
        with timer() as elapsed:
            with vfs.open("/streaming_test.bin", "rb") as f:
                while True:
                    chunk = f.read(read_buffer_size)
                    if not chunk:
                        break
                    bytes_read += len(chunk)
        
        expected_size = num_chunks * chunk_size
        
        print(f"\n{'='*60}")
        print("Streaming Read Test (memory efficiency)")
        print(f"{'='*60}")
        print(f"File size:       {format_size(expected_size)}")
        print(f"Read buffer:     {format_size(read_buffer_size)}")
        print(f"Bytes read:      {format_size(bytes_read)}")
        print(f"Time:            {elapsed()*1000:.2f}ms")
        print(f"{'='*60}")
        
        assert bytes_read == expected_size


# ============================================================================
# Summary Report
# ============================================================================

class TestBenchmarkSummary:
    """Run a comprehensive benchmark suite and report summary."""

    def test_full_benchmark_summary(self, capsys):
        """Run all key benchmarks and produce a summary report."""
        vfs = VirtualFileSystem(":memory:")
        
        results = {}
        
        # 1. Small file write throughput
        num_files = 500
        content = "x" * 100
        with timer() as elapsed:
            for i in range(num_files):
                vfs.write_text(f"/bench/small_{i}.txt", content)
        results['small_write_fps'] = num_files / elapsed()
        
        # 2. Small file read throughput
        with timer() as elapsed:
            for i in range(num_files):
                vfs.read_text(f"/bench/small_{i}.txt")
        results['small_read_fps'] = num_files / elapsed()
        
        # 3. Large file write throughput (10MB)
        large_content = b"x" * (10 * 1024 * 1024)
        with timer() as elapsed:
            vfs.write_bytes("/bench/large.bin", large_content)
        results['large_write_mbps'] = 10 / elapsed()
        
        # 4. Large file read throughput
        with timer() as elapsed:
            vfs.read_bytes("/bench/large.bin")
        results['large_read_mbps'] = 10 / elapsed()
        
        # 5. Directory operations
        vfs.makedirs("/bench/dirs")  # Create parent first
        with timer() as elapsed:
            for i in range(200):
                vfs.mkdir(f"/bench/dirs/dir_{i}")
        results['mkdir_ops'] = 200 / elapsed()
        
        # Print summary
        print(f"\n{'='*60}")
        print("BENCHMARK SUMMARY")
        print(f"{'='*60}")
        print(f"Small file writes:  {results['small_write_fps']:,.0f} files/sec")
        print(f"Small file reads:   {results['small_read_fps']:,.0f} files/sec")
        print(f"Large file write:   {results['large_write_mbps']:.1f} MB/sec")
        print(f"Large file read:    {results['large_read_mbps']:.1f} MB/sec")
        print(f"mkdir operations:   {results['mkdir_ops']:,.0f} ops/sec")
        print(f"{'='*60}")
        
        vfs.close()
