defmodule LocCheckerTest do
  use ExUnit.Case
  import ExUnit.CaptureIO

  @sample_file Path.join(["test", "fixtures", "sample.ex"])
  @large_file Path.join(["test", "fixtures", "large.ex"])


  describe "remove_excluded_folders/1" do
    test "removes paths containing excluded folder names" do
      paths = [
        "lib/my_app/core.ex",
        # Should be removed (deps)
        "deps/phoenix/lib/phoenix.ex",
        # Should be removed (node_modules)
        "assets/node_modules/jquery/index.js",
        # Should be removed (.git is usually excluded if in folder list)
        ".git/HEAD",
        # Should be removed (build)
        "build/dev/lib/app.beam"
      ]

      result = LocChecker.remove_excluded_folders(paths)

      assert "lib/my_app/core.ex" in result
      refute "deps/phoenix/lib/phoenix.ex" in result
      refute "assets/node_modules/jquery/index.js" in result
      refute "build/dev/lib/app.beam" in result
    end

    test "keeps paths that are not excluded" do
      paths = ["lib/app.ex", "test/app_test.exs"]
      assert LocChecker.remove_excluded_folders(paths) == paths
    end
  end

  describe "remove_files_with_no_file_extension/1" do
    test "removes files like Dockerfile or Makefile that have no extension" do
      paths = [
        "lib/app.ex",
        "Dockerfile",
        "Makefile",
        "README"
      ]

      result = LocChecker.remove_files_with_no_file_extension(paths)

      assert result == ["lib/app.ex"]
    end

    test "keeps files with explicit extensions" do
      # We use a standard file with extension to avoid platform ambiguity on dotfiles
      paths = ["lib/app.ex", "config.json"]
      result = LocChecker.remove_files_with_no_file_extension(paths)
      assert "config.json" in result
    end
  end

  describe "remove_non_code_files/1" do
    test "removes files with extensions in the exclusion list" do
      paths = [
        "lib/app.ex",
        # Excluded
        "priv/static/image.png",
        # Excluded
        "docs/manual.pdf",
        # Excluded
        "logs/error.log",
        # Excluded
        "data.json"
      ]

      result = LocChecker.remove_non_code_files(paths)

      assert "lib/app.ex" in result
      refute "priv/static/image.png" in result
      refute "data.json" in result
    end

    test "keeps files with valid extensions" do
      paths = ["script.rb", "app.js", "main.rs", "lib.ex"]
      # Assuming these aren't in the default excluded list in your module
      result = LocChecker.remove_non_code_files(paths)
      assert length(result) == 4
    end
  end

  # ============================================================================
  # 2. FILE IO TESTS
  # ============================================================================

  describe "get_loc_of_file/1" do
    test "counts lines correctly for a small file" do
      # Verify file existence first to provide better error message
      assert File.exists?(@sample_file), "Test fixture not found at #{@sample_file}"

      # sample.ex has 5 lines based on your upload
      assert LocChecker.get_loc_of_file(@sample_file) == 5
    end

    test "counts lines correctly for a large file" do
      assert File.exists?(@large_file), "Test fixture not found at #{@large_file}"
      assert LocChecker.get_loc_of_file(@large_file) == 1014
    end

    test "returns false (or fails match) for non-existent file" do
      # Based on your implementation using `with`, if File.regular? fails,
      # it falls through. Since there is no `else` block in get_loc_of_file,
      # it returns the value of the failed predicate (false).
      assert LocChecker.get_loc_of_file("non_existent_ghost_file.ex") == false
    end
  end

  describe "check_file/3" do
    test "returns stats for a valid file below threshold" do
      assert File.exists?(@sample_file), "Test fixture not found at #{@sample_file}"

      stats = LocChecker.check_file(@sample_file, false, 1000)

      # If stats is nil, it means File.regular? failed inside check_file
      refute is_nil(stats), "check_file returned nil (File not found?)"
      assert stats.file == @sample_file
      assert stats.loc == 5
      assert stats.above_threshold == false
    end

    test "detects file above threshold" do
      assert File.exists?(@large_file), "Test fixture not found at #{@large_file}"
      stats = LocChecker.check_file(@large_file, false, 500)

      refute is_nil(stats), "check_file returned nil"
      assert stats.file == @large_file
      assert stats.loc == 1014
      assert stats.above_threshold == true
    end

    test "prints warning to stdout when above threshold" do
      assert File.exists?(@large_file)

      output =
        capture_io(fn ->
          LocChecker.check_file(@large_file, false, 500)
        end)

      assert output =~ "[WARN]"
      assert output =~ @large_file
      assert output =~ "(above 500)"
    end

    test "prints OK to stdout when below threshold AND show_files_below_loc is true" do
      assert File.exists?(@sample_file)

      output =
        capture_io(fn ->
          LocChecker.check_file(@sample_file, true, 1000)
        end)

      assert output =~ "[OK]"
      assert output =~ @sample_file
    end

    test "prints nothing when below threshold AND show_files_below_loc is false" do
      assert File.exists?(@sample_file)

      output =
        capture_io(fn ->
          LocChecker.check_file(@sample_file, false, 1000)
        end)

      assert output == ""
    end
  end

  # ============================================================================
  # 3. INTEGRATION TESTS (Simulated Folder Scan)
  # ============================================================================

  describe "check_folder/3" do
    @test_dir "sandbox_test_scan"

    setup do
      # Setup: Create a fake directory structure
      File.mkdir_p!("#{@test_dir}/lib")
      File.mkdir_p!("#{@test_dir}/deps/fake_dep")
      File.mkdir_p!("#{@test_dir}/assets")

      # 1. Valid Code File
      # 10 LOC
      File.write!("#{@test_dir}/lib/valid.ex", Enum.join(1..10, "\n"))

      # 2. Valid Code File (Large)
      # 100 LOC
      File.write!("#{@test_dir}/lib/large.ex", Enum.join(1..100, "\n"))

      # 3. Ignored File (Txt)
      File.write!("#{@test_dir}/readme.txt", "Ignored")

      # 4. Ignored Folder (deps)
      File.write!("#{@test_dir}/deps/fake_dep/lib.ex", "Should be ignored")

      # 5. Ignored No Extension
      File.write!("#{@test_dir}/LICENSE", "MIT")

      on_exit(fn ->
        File.rm_rf(@test_dir)
      end)

      :ok
    end

    test "scans folder, ignores garbage, and calculates correct stats" do
      # Fix for "BadMapError": `with_io` returns the output string, NOT the function result.
      # We use message passing to get the result out of the capture_io block.

      parent = self()
      ref = make_ref()

      output =
        capture_io(fn ->
          result = LocChecker.check_folder(@test_dir, false, 50)
          send(parent, {ref, result})
        end)

      # Receive the result from inside the capture block
      result =
        receive do
          {^ref, res} -> res
        after
          1000 -> flunk("Timeout: check_folder did not return a result")
        end

      # Assert on the returned Map structure
      assert is_map(result), "Expected result to be a map, got: #{inspect(result)}"
      # Only valid.ex and large.ex
      assert result.total_files == 2
      # 10 + 100
      assert result.total_loc == 110
      # 110 / 2
      assert result.avg_loc == 55.0

      # Verify the file list in result
      files = Enum.map(result.files, & &1.file)
      # Fix path separators for assertions if needed, though usually fine
      assert "#{@test_dir}/lib/valid.ex" in files
      assert "#{@test_dir}/lib/large.ex" in files
      # Text file excluded
      refute "#{@test_dir}/readme.txt" in files
      # Folder excluded
      refute "#{@test_dir}/deps/fake_dep/lib.ex" in files

      # Verify Output Summary
      assert output =~ "Files scanned: 2"
      assert output =~ "Total LOC: 110"
      assert output =~ "Largest file: #{@test_dir}/lib/large.ex (100 lines)"

      # Verify Threshold Warning in Output (large.ex is 100, threshold is 50)
      assert output =~ "Files above threshold (50): 1"
    end
  end
end
