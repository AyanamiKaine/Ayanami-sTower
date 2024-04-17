import Config

config :stella,
  ecto_repos: [Stella.Repo]

config :stella, Stella.Repo,
  temp_store: :file,
  database: "./stella.db"
