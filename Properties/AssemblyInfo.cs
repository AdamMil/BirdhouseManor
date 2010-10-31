using System.Reflection;
using System.Runtime.InteropServices;

[assembly: AssemblyTitle("Birdhouse Manor")]
[assembly: AssemblyDescription("A PC port of the D&D board games from Wizards of the Coast, like Castle Ravenloft.")]
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif
[assembly: AssemblyProduct("Birdhouse Manor")]
[assembly: AssemblyCopyright("Copyright © Adam Milazzo 2010")]

[assembly: ComVisible(false)]

[assembly: AssemblyVersion("0.1.0.*")]
[assembly: AssemblyFileVersion("0.1.0.0")]
