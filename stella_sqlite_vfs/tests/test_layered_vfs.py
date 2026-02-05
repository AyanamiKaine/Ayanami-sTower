"""
Tests for VFS underlay/layering feature - overlay filesystem for mod/DLC support.

This tests the integrated underlay feature in VirtualFileSystem, which provides
layered file resolution without a separate LayeredVFS class.

Run with: pytest tests/test_layered_vfs.py -v
"""

import pytest
import threading

from lib.vfs import VirtualFileSystem


# ============================================================================
# Fixtures
# ============================================================================

@pytest.fixture
def base_vfs():
    """Base game VFS with some assets."""
    vfs = VirtualFileSystem(":memory:")
    vfs.makedirs("/textures")
    vfs.makedirs("/sounds")
    vfs.makedirs("/scripts")
    
    vfs.write_text("/textures/sword.png", "base_sword_texture")
    vfs.write_text("/textures/shield.png", "base_shield_texture")
    vfs.write_text("/textures/helmet.png", "base_helmet_texture")
    vfs.write_text("/sounds/hit.wav", "base_hit_sound")
    vfs.write_text("/scripts/main.lua", "base_main_script")
    vfs.write_text("/config.json", '{"version": "1.0"}')
    
    return vfs


@pytest.fixture
def dlc_vfs():
    """DLC VFS with HD textures."""
    vfs = VirtualFileSystem(":memory:")
    vfs.makedirs("/textures")
    
    # HD versions of some textures
    vfs.write_text("/textures/sword.png", "dlc_hd_sword_texture")
    vfs.write_text("/textures/shield.png", "dlc_hd_shield_texture")
    # New DLC-only content
    vfs.write_text("/textures/dragon.png", "dlc_dragon_texture")
    
    return vfs


@pytest.fixture
def mod_vfs():
    """User mod VFS (this will be the top layer)."""
    vfs = VirtualFileSystem(":memory:")
    vfs.makedirs("/textures")
    vfs.makedirs("/custom")
    
    # Mod replaces sword texture
    vfs.write_text("/textures/sword.png", "mod_custom_sword_texture")
    # Mod adds new content
    vfs.write_text("/custom/my_item.png", "mod_custom_item")
    
    return vfs


@pytest.fixture
def layered(base_vfs, dlc_vfs, mod_vfs):
    """
    VFS with underlay hierarchy:
    - mod_vfs (top/local - writable)
    - dlc_vfs (underlay, priority=10)
    - base_vfs (underlay, priority=0)
    """
    # mod_vfs is the top layer, add base and dlc as underlays
    mod_vfs.add_underlay(dlc_vfs, priority=10, name="dlc")
    mod_vfs.add_underlay(base_vfs, priority=0, name="base")
    return mod_vfs


# ============================================================================
# Basic Underlay Tests
# ============================================================================

class TestUnderlayManagement:
    """Test underlay management."""
    
    def test_add_underlay_basic(self, base_vfs):
        game = VirtualFileSystem(":memory:")
        name = game.add_underlay(base_vfs, priority=0, name="base")
        assert name == "base"
        assert len(game.list_layers()) == 2  # local + base
    
    def test_add_underlay_auto_name(self, base_vfs):
        game = VirtualFileSystem(":memory:")
        name = game.add_underlay(base_vfs)
        assert name == "underlay_0"
    
    def test_add_underlay_duplicate_name_fails(self, base_vfs, dlc_vfs):
        game = VirtualFileSystem(":memory:")
        game.add_underlay(base_vfs, name="same")
        with pytest.raises(ValueError, match="already exists"):
            game.add_underlay(dlc_vfs, name="same")
    
    def test_remove_underlay(self, base_vfs):
        game = VirtualFileSystem(":memory:")
        game.add_underlay(base_vfs, name="base")
        assert game.remove_underlay("base") is True
        assert game.remove_underlay("nonexistent") is False
        assert len(game.list_layers()) == 1  # just local
    
    def test_list_layers(self, layered):
        layers = layered.list_layers()
        assert len(layers) == 3
        
        # Local is always first
        assert layers[0]['name'] == 'local'
        assert layers[0]['writable'] is True
        
        # Then underlays by priority (higher first)
        assert layers[1]['name'] == 'dlc'
        assert layers[1]['priority'] == 10
        assert layers[2]['name'] == 'base'
        assert layers[2]['priority'] == 0


# ============================================================================
# Overlay Resolution Tests
# ============================================================================

class TestOverlayResolution:
    """Test that files resolve from correct layers."""
    
    def test_base_only_file(self, layered):
        """File only in base should come from base."""
        content = layered.read_text("/textures/helmet.png")
        assert content == "base_helmet_texture"
    
    def test_dlc_overrides_base(self, layered):
        """DLC version should override base for shield."""
        content = layered.read_text("/textures/shield.png")
        assert content == "dlc_hd_shield_texture"
    
    def test_local_overrides_all(self, layered):
        """Local (mod) version should override both DLC and base for sword."""
        content = layered.read_text("/textures/sword.png")
        assert content == "mod_custom_sword_texture"
    
    def test_dlc_only_file(self, layered):
        """DLC-only file should be accessible."""
        content = layered.read_text("/textures/dragon.png")
        assert content == "dlc_dragon_texture"
    
    def test_local_only_file(self, layered):
        """Local-only file should be accessible."""
        content = layered.read_text("/custom/my_item.png")
        assert content == "mod_custom_item"
    
    def test_which_layer(self, layered):
        """Debug helper should identify correct source."""
        assert layered.which_layer("/textures/sword.png") == "local"
        assert layered.which_layer("/textures/shield.png") == "dlc"
        assert layered.which_layer("/textures/helmet.png") == "base"
        assert layered.which_layer("/nonexistent.png") is None
    
    def test_which_layer_detailed(self, layered):
        """Detailed resolution should show all layers."""
        info = layered.which_layer_detailed("/textures/sword.png")
        
        assert info['resolved_from'] == 'local'
        assert len(info['layers']) == 3
        
        # Check each layer
        layer_names = [l['name'] for l in info['layers']]
        assert 'local' in layer_names
        assert 'dlc' in layer_names
        assert 'base' in layer_names


# ============================================================================
# Write Operations Tests
# ============================================================================

class TestWriteOperations:
    """Test writes go to local layer."""
    
    def test_write_to_local_layer(self, layered):
        """Writes should go to the local layer."""
        layered.write_text("/new_file.txt", "new content")
        
        # Should be readable
        assert layered.read_text("/new_file.txt") == "new content"
        
        # Should have gone to local layer
        assert layered.which_layer("/new_file.txt") == "local"
    
    def test_copy_on_write_pattern(self, layered):
        """Copy base file to local layer for modification."""
        # Read from base
        original = layered.read_text("/sounds/hit.wav")
        assert original == "base_hit_sound"
        assert layered.which_layer("/sounds/hit.wav") == "base"
        
        # Write to local layer (shadows the base version)
        layered.write_text("/sounds/hit.wav", "modded_hit_sound")
        
        # Now comes from local
        assert layered.read_text("/sounds/hit.wav") == "modded_hit_sound"
        assert layered.which_layer("/sounds/hit.wav") == "local"
    
    def test_mkdir_in_local_layer(self, layered):
        """mkdir should create in local layer."""
        layered.mkdir("/new_folder")
        assert layered.isdir("/new_folder")


# ============================================================================
# Directory Listing Tests
# ============================================================================

class TestDirectoryListing:
    """Test merged directory listings."""
    
    def test_listdir_merged(self, layered):
        """listdir should merge entries from all layers."""
        entries = layered.listdir("/textures")
        
        # Should have files from all layers
        assert "sword.png" in entries      # All three
        assert "shield.png" in entries     # Base + DLC
        assert "helmet.png" in entries     # Base only
        assert "dragon.png" in entries     # DLC only
    
    def test_listdir_no_duplicates(self, layered):
        """Merged listing should not have duplicates."""
        entries = layered.listdir("/textures")
        assert len(entries) == len(set(entries))
    
    def test_listdir_sorted(self, layered):
        """Merged listing should be sorted."""
        entries = layered.listdir("/textures")
        assert entries == sorted(entries)
    
    def test_walk_merged(self, layered):
        """walk should include directories from all layers."""
        all_dirs = []
        all_files = []
        
        for dirpath, dirs, files in layered.walk("/"):
            all_dirs.extend(dirs)
            all_files.extend(files)
        
        # Should see custom dir from local
        assert "custom" in all_dirs
        # Should see scripts dir from base
        assert "scripts" in all_dirs
        # Should see files from all layers
        assert "config.json" in all_files


# ============================================================================
# File Query Tests
# ============================================================================

class TestFileQueries:
    """Test exists, isfile, isdir, stat."""
    
    def test_exists_in_any_layer(self, layered):
        assert layered.exists("/textures/sword.png")    # All
        assert layered.exists("/textures/dragon.png")   # DLC
        assert layered.exists("/custom/my_item.png")    # Local
        assert not layered.exists("/nonexistent.png")
    
    def test_isfile(self, layered):
        assert layered.isfile("/textures/sword.png")
        assert not layered.isfile("/textures")
        assert not layered.isfile("/nonexistent.png")
    
    def test_isdir(self, layered):
        assert layered.isdir("/textures")
        assert layered.isdir("/custom")
        assert layered.isdir("/scripts")  # From base
        assert not layered.isdir("/textures/sword.png")
    
    def test_stat_includes_source(self, layered):
        """stat should indicate which layer the file came from."""
        stat = layered.stat("/textures/sword.png")
        assert stat.source_layer == 'local'
        
        stat = layered.stat("/textures/dragon.png")
        assert stat.source_layer == 'dlc'
        
        stat = layered.stat("/textures/helmet.png")
        assert stat.source_layer == 'base'


# ============================================================================
# Glob Pattern Tests
# ============================================================================

class TestGlobPatterns:
    """Test glob across layers."""
    
    def test_glob_all_layers(self, layered):
        """glob should find files from all layers."""
        matches = layered.glob("/textures/*.png")
        
        assert "/textures/sword.png" in matches
        assert "/textures/dragon.png" in matches  # DLC
        assert "/textures/helmet.png" in matches  # Base
    
    def test_glob_no_duplicates(self, layered):
        """glob should not return duplicates."""
        matches = layered.glob("/textures/*.png")
        assert len(matches) == len(set(matches))


# ============================================================================
# Error Handling Tests
# ============================================================================

class TestErrorHandling:
    """Test error conditions."""
    
    def test_file_not_found(self, layered):
        with pytest.raises(FileNotFoundError):
            layered.read_text("/nonexistent.txt")
    
    def test_directory_not_found(self, layered):
        with pytest.raises(FileNotFoundError):
            layered.listdir("/nonexistent/path")


# ============================================================================
# Thread Safety Tests
# ============================================================================

class TestThreadSafety:
    """Test concurrent access."""
    
    def test_concurrent_reads(self, layered):
        """Multiple threads reading simultaneously."""
        results = []
        errors = []
        
        def reader(thread_id):
            try:
                for _ in range(50):
                    content = layered.read_text("/textures/sword.png")
                    results.append((thread_id, content))
            except Exception as e:
                errors.append(e)
        
        threads = [threading.Thread(target=reader, args=(i,)) for i in range(4)]
        for t in threads:
            t.start()
        for t in threads:
            t.join()
        
        assert len(errors) == 0
        assert len(results) == 200
        assert all(content == "mod_custom_sword_texture" for _, content in results)


# ============================================================================
# Real-World Scenario Tests
# ============================================================================

class TestRealWorldScenarios:
    """Test realistic game modding scenarios."""
    
    def test_mod_load_order(self):
        """Test typical mod load order scenario."""
        # Base game
        base = VirtualFileSystem(":memory:")
        base.write_text("/items/sword.json", '{"damage": 10}')
        base.write_text("/items/shield.json", '{"defense": 5}')
        
        # Balance patch mod (medium priority underlay)
        balance = VirtualFileSystem(":memory:")
        balance.write_text("/items/sword.json", '{"damage": 8}')  # Nerfed
        
        # OP weapon mod (high priority underlay)  
        op_mod = VirtualFileSystem(":memory:")
        op_mod.write_text("/items/sword.json", '{"damage": 999}')  # OP
        
        # Game is the top layer (empty, just orchestrates)
        game = VirtualFileSystem(":memory:")
        game.add_underlay(op_mod, priority=100, name="op_mod")
        game.add_underlay(balance, priority=50, name="balance_patch")
        game.add_underlay(base, priority=0, name="base")
        
        # OP mod wins (highest priority underlay)
        assert '999' in game.read_text("/items/sword.json")
        
        # Remove OP mod
        game.remove_underlay("op_mod")
        assert '8' in game.read_text("/items/sword.json")  # Balance patch
        
        # Remove balance patch too
        game.remove_underlay("balance_patch")
        assert '10' in game.read_text("/items/sword.json")  # Original
    
    def test_dlc_expansion(self):
        """Test DLC adding new content areas."""
        base = VirtualFileSystem(":memory:")
        base.makedirs("/levels")
        base.write_text("/levels/level1.dat", "base_level1")
        base.write_text("/levels/level2.dat", "base_level2")
        
        dlc = VirtualFileSystem(":memory:")
        dlc.makedirs("/levels")
        dlc.write_text("/levels/level3.dat", "dlc_level3")
        dlc.write_text("/levels/level4.dat", "dlc_level4")
        
        # Game with DLC
        game = VirtualFileSystem(":memory:")
        game.add_underlay(dlc, priority=10, name="expansion")
        game.add_underlay(base, priority=0, name="base")
        
        # All levels accessible
        levels = game.listdir("/levels")
        assert set(levels) == {"level1.dat", "level2.dat", "level3.dat", "level4.dat"}
    
    def test_user_save_layer(self):
        """Writable layer for user saves separate from game data."""
        game_data = VirtualFileSystem(":memory:")
        game_data.write_text("/config/defaults.json", '{"volume": 80}')
        
        # User data is the top (writable) layer
        user_data = VirtualFileSystem(":memory:")
        user_data.add_underlay(game_data, priority=0, name="game")
        
        # Read default config (from underlay)
        assert user_data.read_text("/config/defaults.json") == '{"volume": 80}'
        
        # User creates their own config (in local/top layer)
        user_data.makedirs("/config", exist_ok=True)
        user_data.write_text("/config/user.json", '{"volume": 50}')
        
        # Both configs accessible
        assert user_data.exists("/config/defaults.json")  # From underlay
        assert user_data.exists("/config/user.json")       # From local
        
        # User config came from local layer
        assert user_data.which_layer("/config/user.json") == "local"
        assert user_data.which_layer("/config/defaults.json") == "game"


# ============================================================================
# No Underlays - Backward Compatibility
# ============================================================================

class TestBackwardCompatibility:
    """Test that VFS works exactly as before when no underlays are added."""
    
    def test_basic_operations_without_underlays(self):
        """All basic operations should work without underlays."""
        vfs = VirtualFileSystem(":memory:")
        
        # Write
        vfs.write_text("/test.txt", "hello")
        
        # Read
        assert vfs.read_text("/test.txt") == "hello"
        
        # Exists
        assert vfs.exists("/test.txt")
        assert not vfs.exists("/nonexistent.txt")
        
        # Listdir
        assert "test.txt" in vfs.listdir("/")
        
        # Stat
        stat = vfs.stat("/test.txt")
        assert stat.st_size == 5
        assert stat.source_layer == 'local'
    
    def test_list_layers_single(self):
        """list_layers should show just 'local' when no underlays."""
        vfs = VirtualFileSystem(":memory:")
        layers = vfs.list_layers()
        
        assert len(layers) == 1
        assert layers[0]['name'] == 'local'
        assert layers[0]['writable'] is True
