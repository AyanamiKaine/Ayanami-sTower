// By default script directives (#r) are being removed
// from the script before compilation. We are just doing this here 
// so the C# Devkit and Omnisharp (For what every reason the libraries.rsp 
// does not get used any more, new bug?) can provide us with autocompletion and analysis of the code
#r "../bin/Debug/net9.0/Avalonia.Base.dll"
#r "../bin/Debug/net9.0/Avalonia.FreeDesktop.dll"
#r "../bin/Debug/net9.0/Avalonia.dll"
#r "../bin/Debug/net9.0/Avalonia.Desktop.dll"
#r "../bin/Debug/net9.0/Avalonia.X11.dll"
#r "../bin/Debug/net9.0/FluentAvalonia.dll"
#r "../bin/Debug/net9.0/Avalonia.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Markup.Xaml.dll"
#r "../bin/Debug/net9.0/Flecs.NET.dll"
#r "../bin/Debug/net9.0/Flecs.NET.Bindings.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Controls.xml"
#r "../bin/Debug/net9.0/Avalonia.Flecs.FluentUI.Controls.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.StellaLearning.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Scripting.dll"
#r "../bin/Debug/net9.0/Avalonia.Flecs.Scripting.xml"

using Flecs.NET.Core;
using Avalonia.Flecs.Scripting;
using Avalonia.Flecs.Controls.ECS;
using static Avalonia.Flecs.Controls.ECS.Module;
using Avalonia.Controls;
using Avalonia.Data;
using System;
using System.Collections.ObjectModel;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia;
using FluentAvalonia.UI.Controls;
using Avalonia.Flecs.StellaLearning.Data;