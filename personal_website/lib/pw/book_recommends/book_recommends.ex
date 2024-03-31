defmodule Pw.BookRecommends do
  @books [  # <-- Module Attribute
    %{
      title: "Thinking in Systems A Primer",
      author: "Donella Meadows",
      description: "TODO",
      review: "
      The best book to understand even the most complex systems from economic ones to understanding why
      companies fail or how to correctly structure projects.
      "
    },
    %{
      title: "Engineering Systems - Systems Thinking Applied to Safety",
      author: "Nancy G. Leveson",
      description: "Trully understand what the true reasons of failure in complex systems are and how to achieve safety in a system",
      review: "
      Even if all components in a system are reliable and have many redundancies,
      the system itself can be unsafe.
      This is the great insight of Leveson, not only are true causes of system failure
      layed out that go far beyond of pointing at operator failure.
      It also shows good ways to get safety in a system.
      If you believe that operator failure is the most common reason of why systems fail then this book will be a wakeup call for you."
    }
  ]

  def get_all_books() do
    @books
  end
end
