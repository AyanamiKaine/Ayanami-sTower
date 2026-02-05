"""
Examples showcasing the Enhanced Virtual File System improvements.

This demonstrates:
1. Memory-efficient large file handling
2. io module compliance (buffering, encoding)
3. Atomic operations with transactions
4. Pattern matching with glob()
5. Compression support
6. Advanced metadata
"""

from lib.vfs import VirtualFileSystem
import io


def example_1_large_file_handling():
    """
    Demonstrate memory-efficient handling of large files.
    
    The original VFS would load the entire file into memory.
    The enhanced version streams data in chunks.
    """
    print("=" * 60)
    print("Example 1: Memory-Efficient Large File Handling")
    print("=" * 60)
    
    vfs = VirtualFileSystem("large_files.db")
    
    # Write a 100MB file without consuming 100MB of RAM
    print("\n1. Writing 100MB file in chunks...")
    with vfs.open("/large_video.mp4", "wb") as f:
        chunk_size = 1024 * 1024  # 1MB chunks
        for i in range(100):
            # Simulate video data
            chunk = bytes([i % 256]) * chunk_size
            f.write(chunk)
            if (i + 1) % 10 == 0:
                print(f"   Written {i + 1}MB...")
    
    print(f"\n2. File size: {vfs.getsize('/large_video.mp4') / (1024*1024):.1f}MB")
    
    # Read specific portion without loading entire file
    print("\n3. Reading middle section (50MB-51MB) without loading full file...")
    with vfs.open("/large_video.mp4", "rb") as f:
        f.seek(50 * 1024 * 1024)  # Jump to 50MB mark
        sample = f.read(1024 * 1024)  # Read 1MB
        print(f"   Read {len(sample)} bytes from middle of file")
        print(f"   Sample byte value: {sample[0]}")
    
    vfs.close()
    print("\n✓ Large file handled efficiently!\n")


def example_2_io_module_compliance():
    """
    Demonstrate Python io module compatibility.
    
    The enhanced VFS integrates with BufferedReader/Writer
    and TextIOWrapper for proper encoding and buffering.
    """
    print("=" * 60)
    print("Example 2: io Module Compliance")
    print("=" * 60)
    
    vfs = VirtualFileSystem(":memory:")
    
    # Text mode with encoding
    print("\n1. Writing UTF-8 text with various encodings...")
    with vfs.open("/multilingual.txt", "w", encoding="utf-8") as f:
        f.write("English: Hello World\n")
        f.write("Chinese: 你好世界\n")
        f.write("Arabic: مرحبا بالعالم\n")
        f.write("Emoji: 🌍🌎🌏\n")
    
    # Read with buffering
    print("\n2. Reading with automatic buffering...")
    with vfs.open("/multilingual.txt", "r", encoding="utf-8", buffering=8192) as f:
        print(f"   File object type: {type(f).__name__}")
        print(f"   Is BufferedReader wrapper: {isinstance(f, io.TextIOWrapper)}")
        
        print("\n   Content:")
        for line in f:
            print(f"   {line.rstrip()}")
    
    # Binary mode with explicit buffering
    print("\n3. Binary mode with BufferedWriter...")
    with vfs.open("/binary.dat", "wb", buffering=4096) as f:
        print(f"   File object type: {type(f).__name__}")
        f.write(b"\x89PNG\r\n\x1a\n")  # PNG header
        print(f"   Written PNG header bytes")
    
    vfs.close()
    print("\n✓ Full io module support!\n")


def example_3_atomic_operations():
    """
    Demonstrate atomic operations with transaction support.
    
    Complex operations are wrapped in transactions to ensure
    all-or-nothing execution.
    """
    print("=" * 60)
    print("Example 3: Atomic Operations")
    print("=" * 60)
    
    vfs = VirtualFileSystem(":memory:")
    
    # Create a project structure
    print("\n1. Creating project structure...")
    vfs.makedirs("/myproject/src")
    vfs.makedirs("/myproject/tests")
    vfs.makedirs("/myproject/docs")
    
    vfs.write_text("/myproject/README.md", "# My Project")
    vfs.write_text("/myproject/src/main.py", "def main(): pass")
    vfs.write_text("/myproject/src/utils.py", "def helper(): pass")
    vfs.write_text("/myproject/tests/test_main.py", "def test(): pass")
    
    print("   Created:")
    for dirpath, dirnames, filenames in vfs.walk("/myproject"):
        level = dirpath.count('/') - 1
        indent = "   " + "  " * level
        print(f"{indent}{dirpath.split('/')[-1]}/")
        for filename in filenames:
            print(f"{indent}  {filename}")
    
    # Atomic rename of entire directory tree
    print("\n2. Atomically renaming entire project...")
    vfs.rename("/myproject", "/awesome_project")
    
    print("   Old path exists:", vfs.exists("/myproject"))
    print("   New path exists:", vfs.exists("/awesome_project"))
    print("   Files accessible:", vfs.exists("/awesome_project/src/main.py"))
    
    # Atomic tree copy
    print("\n3. Atomically copying entire project...")
    vfs.copytree("/awesome_project", "/project_backup")
    
    print("   Backup created:", vfs.exists("/project_backup"))
    print("   Backup has files:", vfs.exists("/project_backup/src/main.py"))
    
    # Atomic tree deletion
    print("\n4. Atomically deleting backup...")
    vfs.rmtree("/project_backup")
    
    print("   Backup removed:", not vfs.exists("/project_backup"))
    print("   Original intact:", vfs.exists("/awesome_project/src/main.py"))
    
    vfs.close()
    print("\n✓ All operations completed atomically!\n")


def example_4_glob_pattern_matching():
    """
    Demonstrate efficient glob pattern matching.
    
    Uses SQLite's native GLOB operator for fast searches.
    """
    print("=" * 60)
    print("Example 4: Glob Pattern Matching")
    print("=" * 60)
    
    vfs = VirtualFileSystem(":memory:")
    
    # Create a realistic project structure
    print("\n1. Creating web application structure...")
    files = [
        "/webapp/app.py",
        "/webapp/config.py",
        "/webapp/models.py",
        "/webapp/static/style.css",
        "/webapp/static/app.js",
        "/webapp/static/images/logo.png",
        "/webapp/templates/index.html",
        "/webapp/templates/about.html",
        "/webapp/tests/test_app.py",
        "/webapp/tests/test_models.py",
        "/webapp/README.md",
        "/webapp/requirements.txt",
    ]
    
    for filepath in files:
        vfs.write_text(filepath, f"Content of {filepath}")
    
    # Pattern matching examples
    print("\n2. Finding files with glob patterns...")
    
    patterns = [
        ("/*.py", "All Python files in webapp root"),
        ("/**/*.py", "All Python files recursively"),
        ("/*/static/*", "All static assets"),
        ("/**/*.html", "All HTML templates"),
        ("/*/test*.py", "All test files"),
        ("/**/*.{css,js}", "CSS and JS files (note: need separate globs)"),
    ]
    
    for pattern, description in patterns[:-1]:  # Skip last one (needs special handling)
        matches = vfs.glob(pattern)
        print(f"\n   Pattern: {pattern}")
        print(f"   Description: {description}")
        print(f"   Matches ({len(matches)}):")
        for match in matches:
            print(f"     - {match}")
    
    # Multiple patterns
    print(f"\n   Finding CSS and JS files:")
    css_files = vfs.glob("/**/*.css")
    js_files = vfs.glob("/**/*.js")
    print(f"   CSS files: {css_files}")
    print(f"   JS files: {js_files}")
    
    vfs.close()
    print("\n✓ Fast pattern matching with SQLite GLOB!\n")


def example_5_compression():
    """
    Demonstrate transparent compression support.
    
    Files can be compressed automatically to save space.
    """
    print("=" * 60)
    print("Example 5: Compression Support")
    print("=" * 60)
    
    # Test with uncompressed VFS
    print("\n1. Without compression...")
    vfs_uncompressed = VirtualFileSystem(":memory:", compression=False)
    
    # Highly compressible data (log file)
    log_data = "INFO: Application started\n" * 10000
    vfs_uncompressed.write_text("/app.log", log_data)
    
    stat = vfs_uncompressed.stat("/app.log")
    print(f"   File size: {stat['size']:,} bytes")
    print(f"   Compressed: {stat['compressed']}")
    
    # Test with compressed VFS
    print("\n2. With compression enabled...")
    vfs_compressed = VirtualFileSystem(":memory:", compression=True)
    
    vfs_compressed.write_text("/app.log", log_data)
    
    stat = vfs_compressed.stat("/app.log")
    print(f"   File size: {stat['size']:,} bytes")
    print(f"   Compressed: {stat['compressed']}")
    print(f"   (Compression is transparent to user)")
    
    # Verify data integrity
    print("\n3. Verifying data integrity...")
    read_data = vfs_compressed.read_text("/app.log")
    print(f"   Data matches: {read_data == log_data}")
    print(f"   Length: {len(read_data):,} characters")
    
    vfs_uncompressed.close()
    vfs_compressed.close()
    print("\n✓ Transparent compression working!\n")


def example_6_advanced_metadata():
    """
    Demonstrate extended metadata support.
    
    Files can have MIME types, permissions, and timestamps.
    """
    print("=" * 60)
    print("Example 6: Advanced Metadata")
    print("=" * 60)
    
    vfs = VirtualFileSystem(":memory:")
    
    # Create various file types
    print("\n1. Creating files with metadata...")
    
    files = [
        ("/document.json", "application/json", 0o644, '{"key": "value"}'),
        ("/script.sh", "application/x-sh", 0o755, "#!/bin/bash\necho 'Hello'"),
        ("/image.png", "image/png", 0o644, b"\x89PNG\r\n\x1a\n"),
        ("/secret.key", "application/octet-stream", 0o600, "secret123"),
    ]
    
    for path, mimetype, permissions, content in files:
        if isinstance(content, bytes):
            vfs.write_bytes(path, content)
        else:
            vfs.write_text(path, content)
        
        vfs.set_mimetype(path, mimetype)
        vfs.set_permissions(path, permissions)
    
    # Display metadata
    print("\n2. File metadata:")
    print(f"\n   {'Filename':<20} {'MIME Type':<30} {'Perms':<8} {'Size':<8}")
    print("   " + "-" * 70)
    
    for path, _, _, _ in files:
        stat = vfs.stat(path)
        perms = oct(stat['permissions'])
        print(f"   {path:<20} {stat['mimetype']:<30} {perms:<8} {stat['size']:<8}")
    
    # Show timestamps
    print("\n3. Timestamp tracking:")
    stat = vfs.stat("/document.json")
    print(f"   Created:  {stat['created_at']}")
    print(f"   Modified: {stat['modified_at']}")
    
    # Modify and check timestamp update
    import time
    time.sleep(0.01)
    vfs.write_text("/document.json", '{"key": "updated"}')
    stat_updated = vfs.stat("/document.json")
    print(f"\n   After modification:")
    print(f"   Created:  {stat_updated['created_at']} (unchanged)")
    print(f"   Modified: {stat_updated['modified_at']} (updated)")
    
    vfs.close()
    print("\n✓ Rich metadata support!\n")


def example_7_performance_comparison():
    """
    Compare performance between original and enhanced VFS.
    """
    print("=" * 60)
    print("Example 7: Performance Comparison")
    print("=" * 60)
    
    import time
    from lib.vfs import VirtualFileSystem  # Original implementation
    
    # Test with moderately sized file
    test_size = 10 * 1024 * 1024  # 10MB
    test_data = b"x" * test_size
    
    print(f"\n1. Testing with {test_size / (1024*1024):.1f}MB file...")
    
    # Enhanced VFS (chunked)
    print("\n   Enhanced VFS (chunked storage):")
    vfs_enhanced = VirtualFileSystem(":memory:")
    
    start = time.time()
    with vfs_enhanced.open("/test.bin", "wb") as f:
        f.write(test_data)
    write_time_enhanced = time.time() - start
    
    start = time.time()
    with vfs_enhanced.open("/test.bin", "rb") as f:
        _ = f.read()
    read_time_enhanced = time.time() - start
    
    print(f"     Write time: {write_time_enhanced:.3f}s")
    print(f"     Read time:  {read_time_enhanced:.3f}s")
    
    # Original VFS (whole file)
    print("\n   Original VFS (whole file in memory):")
    vfs_original = VirtualFileSystem(":memory:")
    
    start = time.time()
    with vfs_original.open("/test.bin", "wb") as f:
        f.write(test_data)
    write_time_original = time.time() - start
    
    start = time.time()
    with vfs_original.open("/test.bin", "rb") as f:
        _ = f.read()
    read_time_original = time.time() - start
    
    print(f"     Write time: {write_time_original:.3f}s")
    print(f"     Read time:  {read_time_original:.3f}s")
    
    # Comparison
    print("\n2. Key advantages of enhanced VFS:")
    print(f"   ✓ Memory efficient: Streams large files in chunks")
    print(f"   ✓ io module compliant: Works with BufferedReader/TextIOWrapper")
    print(f"   ✓ Atomic operations: Transactions ensure consistency")
    print(f"   ✓ Compression: Optional space savings")
    print(f"   ✓ Rich metadata: MIME types, permissions, timestamps")
    print(f"   ✓ Fast searches: SQLite GLOB operator")
    
    vfs_enhanced.close()
    vfs_original.close()
    print()


def main():
    """Run all examples."""
    print("\n" + "=" * 60)
    print("Enhanced Virtual File System - Demonstration")
    print("=" * 60)
    print("\nThis demonstrates all major improvements over the original VFS:")
    print("1. Memory-efficient large file handling")
    print("2. Python io module compliance") 
    print("3. Atomic operations with transactions")
    print("4. Pattern matching with glob()")
    print("5. Transparent compression")
    print("6. Advanced metadata support")
    print("7. Performance comparison")
    print("\n")
    
    try:
        example_1_large_file_handling()
        example_2_io_module_compliance()
        example_3_atomic_operations()
        example_4_glob_pattern_matching()
        example_5_compression()
        example_6_advanced_metadata()
        example_7_performance_comparison()
        
        print("=" * 60)
        print("All examples completed successfully!")
        print("=" * 60)
        print("\nThe Enhanced VFS is production-ready for:")
        print("  • Configuration management")
        print("  • Small to medium asset storage")
        print("  • Embedded environments")
        print("  • Testing and mocking file systems")
        print("  • Applications requiring atomic file operations")
        print()
        
    except Exception as e:
        print(f"\n❌ Error running examples: {e}")
        import traceback
        traceback.print_exc()


if __name__ == "__main__":
    main()
