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
      If you believe that operator failure is the most common reason of
      why systems fail then this book will be a wakeup call for you."
    },
    %{
      title: "Notes on the Synthesis of Form",
      author: "Christopher Alexander",
      description: "TODO",
      review: "
      Synthesis of Form gives great insights on design,
      no other book teached me more on designing complex systems."
    },
    %{
      title: "Test Driven Development by Example",
      author: "Kent Beck",
      description: "Todo",
      review: "
      If you dont write tests before implementations and dont see any value in it than this book is for you.
      TDD by example is the OG book about it.
      Dont be scared by its release date, its still
      the cannonical refrence about TDD as its the book that coined the term.
      "
    },
    %{
      title: "A Philosophy of Software Design",
      author: "John Ousterhout",
      description: "",
      review: "Designing software is hard, many people skip any form of design or architecture because they
      simply dont know better. 'A Philosophy of Software Design' finally fills the cap of not knowing how to
      design a solution for a problem that will hold in moments of change.
      "
    },
    %{
      title: "Refactoring: Improving the Design of Existing Code",
      author: "Martin Fowler",
      description: "",
      review: "Often times when we look at our code we know we did something ugly but we cant name it or even
      see a better way of doing it. This is where 'Refactoring' helps it defines various 'code smells' that show
      that your code is in need of improvment and it defines and explaines various solutions."
    },
    %{
      title: "Tidy First?: A Personal Exercise in Empirical Software Design",
      author: "Kent Beck",
      description: "",
      review: "A simple solution oriented book to code smells
      similar to 'Refactoring' by Martin Fowler. Short and concise. Its a great addition to become a better
      programmer."
    },
    %{
      title: "Thus Spoke Zarathustra: A Book for All and None",
      author: "Friedrich Nietzsche",
      description: "",
      review: "Not many books challange entire world views we had for ages, not many
      books allow for views outside the world itself. Nietzsche provides a view point
      about the world that is not incremental but radically different. The value of Nietzsche lies in his unique
      view point. I encourage everyone atleast seeking new world views there is no need
      to embrace anything in the world. challenge what you believe should be challenged,
      but never follow something without challenging it first.",
    },
    %{
      title: "Large-Scale C++: Process and Architecture: Process and Architecture",
      author: "John Lakos",
      description: "",
      review: "Writing software at large scales of million lines of code with many different
      applications is probably the highest feat of any programmer team. Lakos gives his great experience
      in creating and maintaining such software, as well as clear design guidlines to
      achive the goal of creating large-scale software. Many design guidlines are
      applicable for other languages than C++, so if you only program in language X,
      this book is still a great read."
    },
    %{
      title: "Uncommon Sense Teaching: Practical Insights in Brain Science to Help Students Learn",
      author: "",
      description: "",
      review: "A perfect modern neuro-science oriented book on teaching and studying. If you have questions like:
      How can i learn X?, How can i teach someone Y? Then this book is for you."
    }
  ]

  def get_all_books() do
    @books
  end
end
