defmodule LocChecker do
  @excluded_file_extensions [
    ".txt",
    ".css",
    ".html",
    ".git",
    ".pdf",
    ".zip",
    ".db",
    ".jpeg",
    ".png",
    ".lock",
    ".log",
    ".bak",
    ".tmp",
    ".swp",
    ".swo",
    ".pyc",
    ".class",
    ".jar",
    ".o",
    ".so",
    ".dll",
    ".dylib",
    ".pem",
    ".crt",
    ".json",
    ".md",
    ".mdx",
    ".gitignore",
    ".env",
    "Dockerfile",
    ".mjs",
    ".webp",
    ".wasm",
    ".exe"
  ]

  @excluded_folders [
    "node_modules",
    "build",
    "dist",
    "deps",
    ".idea",
    ".vscode",
    "__pycache__",
    "venv",
    ".venv",
    ".cache",
    "tmp",
    "log",
    "coverage",
    "vendor",
    "public",
    ".pytest_cache",
    ".parcel-cache"
  ]

  def main(args) do
    case args do
      [folder_path] ->
        LocChecker.check_folder(folder_path)

      _ ->
        IO.puts("Usage: loc_checker <folderPath>")
    end
  end

  @doc """
  Checks the folder for lines of code and prints per file information.
  If `show_files_below_loc?` is set true, it prints files that are below threshold too.
  Optional `threshold` parameter can change the maximum allowed LOC (default 1000).
  Returns a summary map with counts and aggregate values.
  """
  def check_folder(folder_path, show_files_below_loc? \\ false, threshold \\ 1000) do
    files =
      folder_path
      |> Path.join("**")
      |> Path.wildcard()
      |> remove_excluded_folders
      |> remove_non_code_files
      |> remove_files_with_no_file_extension

    stats =
      files
      |> Enum.map(fn file_path ->
        LocChecker.check_file(file_path, show_files_below_loc?, threshold)
      end)
      |> Enum.reject(&is_nil/1)

    total_files = Enum.count(stats)
    total_loc = Enum.reduce(stats, 0, fn %{:loc => l}, acc -> acc + l end)
    avg_loc = if total_files > 0, do: Float.round(total_loc / total_files, 1), else: 0
    files_above = Enum.filter(stats, fn %{:above_threshold => v} -> v end)
    largest = Enum.max_by(stats, fn %{:loc => l} -> l end, fn -> nil end)

    IO.puts("\n===== LOC Check Summary =====")
    IO.puts("Files scanned: #{total_files}")
    IO.puts("Total LOC: #{total_loc}")
    IO.puts("Average LOC: #{avg_loc}")
    IO.puts("Files above threshold (#{threshold}): #{Enum.count(files_above)}")

    if largest != nil do
      IO.puts("Largest file: #{largest.file} (#{largest.loc} lines)")
    end

    IO.puts("============================\n")

    %{files: stats, total_files: total_files, total_loc: total_loc, avg_loc: avg_loc}
  end

  def check_file(file_path, show_files_below_loc? \\ false, threshold \\ 1000) do
    with true <- File.regular?(file_path) do
      loc = get_loc_of_file(file_path)
      above = loc > threshold

      if above do
        IO.puts(
          "#{IO.ANSI.red()}[WARN]#{IO.ANSI.reset()} #{file_path} - LOC: #{IO.ANSI.yellow()}#{loc}#{IO.ANSI.reset()} (above #{threshold})"
        )
      else
        if show_files_below_loc? do
          IO.puts("#{IO.ANSI.green()}[OK]#{IO.ANSI.reset()} #{file_path} - LOC: #{loc}")
        end
      end

      %{file: file_path, loc: loc, above_threshold: above}
    else
      _ -> nil
    end
  end

  def get_loc_of_file(file_path) do
    with true <- File.regular?(file_path) do
      File.stream!(file_path)
      |> Stream.with_index()
      |> Enum.to_list()
      |> Enum.count()
    end
  end

  def get_excluded_file_extensions do
    @excluded_file_extensions
  end

  def get_excluded_folders do
    @excluded_folders
  end

  def remove_excluded_folders(folder_paths) do
    Enum.filter(folder_paths, fn folder_path ->
      !String.contains?(folder_path, @excluded_folders)
    end)
  end

  def remove_files_with_no_file_extension(file_paths) do
    file_paths
    |> Enum.filter(fn file_path -> Path.extname(file_path) != "" end)
  end

  def remove_non_code_files(file_paths) do
    # We get all file extensions
    file_extensions =
      for file_paths <- file_paths do
        Path.extname(file_paths)
      end

    # We remove all excluded file extensions from the file extensions we have
    valid_file_extensions =
      file_extensions
      |> Enum.uniq()
      |> Enum.filter(fn extension -> !(extension in @excluded_file_extensions) end)

    # We use only our valid file extensions that remain to filter out all other
    # The result is a list of filepaths that only contain file paths with a valid file extension
    file_paths
    |> Enum.filter(fn file_path -> Path.extname(file_path) in valid_file_extensions end)
  end
end
