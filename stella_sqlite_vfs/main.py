"""
Example demonstrating the SQLite-backed Virtual File System.

This shows how the VFS can be used as a drop-in replacement for standard
file operations, with all data stored in a SQLite database.
"""

from lib.vfs import VirtualFileSystem


def main():
    # Create a virtual file system (use ":memory:" for in-memory, or a path for persistent storage)
    print("=== SQLite Virtual File System Demo ===\n")

    # Using in-memory database for demo
    with VirtualFileSystem(":memory:") as vfs:
        # --- Creating directories ---
        print("1. Creating directories...")
        vfs.mkdir("/documents")
        vfs.makedirs("/projects/python/src", exist_ok=True)
        print(f"   Created: /documents, /projects/python/src")

        # --- Writing files using file-like interface ---
        print("\n2. Writing files with file-like interface...")
        with vfs.open("/documents/readme.txt", "w") as f:
            f.write("Welcome to the Virtual File System!\n")
            f.write("This file is stored in SQLite, not on disk.\n")
        print("   Wrote: /documents/readme.txt")

        # --- Writing files using convenience methods ---
        print("\n3. Writing files with convenience methods...")
        vfs.write_text("/documents/notes.txt", "These are my notes.\nLine 2.\nLine 3.")
        vfs.write_bytes("/documents/data.bin", b"\x00\x01\x02\x03\x04\x05")
        print("   Wrote: /documents/notes.txt, /documents/data.bin")

        # --- Reading files ---
        print("\n4. Reading files...")
        with vfs.open("/documents/readme.txt", "r") as f:
            content = f.read()
            print(f"   readme.txt content:\n   {content.strip()}")

        # Using convenience methods
        notes = vfs.read_text("/documents/notes.txt")
        print(f"\n   notes.txt content: {notes.splitlines()[0]}...")

        binary_data = vfs.read_bytes("/documents/data.bin")
        print(f"   data.bin content: {binary_data.hex()}")

        # --- Iterating over lines ---
        print("\n5. Iterating over lines...")
        with vfs.open("/documents/notes.txt", "r") as f:
            for i, line in enumerate(f, 1):
                print(f"   Line {i}: {line.strip()}")

        # --- File seeking and telling ---
        print("\n6. Seeking and telling...")
        with vfs.open("/documents/readme.txt", "r") as f:
            print(f"   Initial position: {f.tell()}")
            f.seek(10)
            print(f"   After seek(10): {f.tell()}")
            chunk = f.read(5)
            print(f"   Read 5 chars: '{chunk}'")
            print(f"   Position now: {f.tell()}")

        # --- Append mode ---
        print("\n7. Appending to files...")
        with vfs.open("/documents/notes.txt", "a") as f:
            f.write("\nLine 4 (appended)")
        print(f"   Appended line to notes.txt")
        print(f"   New content: {vfs.read_text('/documents/notes.txt')}")

        # --- Listing directories ---
        print("\n8. Listing directories...")
        print(f"   Root contents: {vfs.listdir('/')}")
        print(f"   /documents contents: {vfs.listdir('/documents')}")

        # --- Walking directory tree ---
        print("\n9. Walking directory tree...")
        for dirpath, dirnames, filenames in vfs.walk("/"):
            print(f"   {dirpath}/")
            for d in dirnames:
                print(f"      [dir]  {d}/")
            for f in filenames:
                print(f"      [file] {f}")

        # --- File/directory checks ---
        print("\n10. Checking paths...")
        print(f"   exists('/documents'): {vfs.exists('/documents')}")
        print(f"   isdir('/documents'): {vfs.isdir('/documents')}")
        print(f"   isfile('/documents'): {vfs.isfile('/documents')}")
        print(f"   isfile('/documents/readme.txt'): {vfs.isfile('/documents/readme.txt')}")
        print(f"   exists('/nonexistent'): {vfs.exists('/nonexistent')}")

        # --- File statistics ---
        print("\n11. File statistics...")
        stat = vfs.stat("/documents/readme.txt")
        print(f"   readme.txt stats:")
        print(f"      size: {stat.st_size} bytes")
        print(f"      created: {stat.st_ctime}")
        print(f"      modified: {stat.st_mtime}")

        # --- Copying files ---
        print("\n12. Copying files...")
        vfs.copy("/documents/readme.txt", "/documents/readme_backup.txt")
        print(f"   Copied readme.txt to readme_backup.txt")
        print(f"   /documents now: {vfs.listdir('/documents')}")

        # --- Renaming/moving files ---
        print("\n13. Renaming files...")
        vfs.rename("/documents/notes.txt", "/documents/my_notes.txt")
        print(f"   Renamed notes.txt to my_notes.txt")
        print(f"   /documents now: {vfs.listdir('/documents')}")

        # --- Removing files ---
        print("\n14. Removing files...")
        vfs.remove("/documents/readme_backup.txt")
        print(f"   Removed readme_backup.txt")
        print(f"   /documents now: {vfs.listdir('/documents')}")

        # --- Copy directory tree ---
        print("\n15. Copying directory tree...")
        vfs.copytree("/documents", "/documents_backup")
        print(f"   Copied /documents to /documents_backup")
        print(f"   /documents_backup contents: {vfs.listdir('/documents_backup')}")

        # --- Remove directory tree ---
        print("\n16. Removing directory tree...")
        vfs.rmtree("/documents_backup")
        print(f"   Removed /documents_backup")
        print(f"   Root now: {vfs.listdir('/')}")

        print("\n=== Demo Complete ===")

    # --- Persistent storage example ---
    print("\n\n=== Persistent Storage Example ===")

    # Create a persistent VFS
    db_file = "my_vfs.db"
    vfs = VirtualFileSystem(db_file)
    vfs.write_text("/config.json", '{"setting": "value"}')
    print(f"Wrote config to {db_file}")
    vfs.close()

    # Reopen and verify data persists
    vfs = VirtualFileSystem(db_file)
    config = vfs.read_text("/config.json")
    print(f"Read config back: {config}")
    vfs.close()

    # Clean up the demo database file
    import os
    os.remove(db_file)
    print(f"Cleaned up {db_file}")


if __name__ == "__main__":
    main()
