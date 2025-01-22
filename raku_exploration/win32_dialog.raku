use NativeCall;

# Define constants for MessageBox flags
constant MB_OK           = 0x00000000;
constant MB_OKCANCEL     = 0x00000001;
constant MB_YESNO        = 0x00000004;
constant MB_YESNOCANCEL  = 0x00000003;
constant MB_ICONERROR    = 0x00000010;
constant MB_ICONWARNING  = 0x00000030;
constant MB_ICONQUESTION = 0x00000020;
constant MB_ICONINFO     = 0x00000040;

# Define constants for return values
constant IDOK     = 1;
constant IDCANCEL = 2;
constant IDYES    = 6;
constant IDNO     = 7;

# Define the MessageBoxW function using NativeCall
sub MessageBoxW(
    uint32 :$hWnd,        # Handle to the parent window (0 for no parent)
    utf16    :$lpText,      # Message text (UTF-16 encoded string)
    utf16    :$lpCaption,   # Title bar text (UTF-16 encoded string)
    uint32 :$uType        # Flags for buttons and icon
) returns uint32 is native('user32') { * }

=begin comment

On the side note, you must overwrite the DPI settings of the RAKU.exe why? Because otherwise the normal DPI of 98 is assumed making the text on modern displays blurry...

=end comment


# Example Usage:
my $result = MessageBoxW(
    hWnd      => 0,
    lpText    => "Text".encode('UTF-16'),
    
=begin comment
In windows we often need wide UTF-16 strings, not normal c-strings. We use rakus.encode('UTF-16') to return a buffer that has the right UTF-16 string windows can understand. Its the equivalent to saying L"Text" in C/C++.
=end comment

    lpCaption => "Caption".encode('UTF-16'),
    uType     => MB_YESNOCANCEL + MB_ICONINFO
);

# Handle the result
given $result {
    when IDOK     { say "OK was clicked." }
    when IDCANCEL { say "Cancel was clicked." }
    when IDYES    { say "Yes was clicked." }
    when IDNO     { say "No was clicked." }
    default       { say "Unknown button clicked: $_" }
}
