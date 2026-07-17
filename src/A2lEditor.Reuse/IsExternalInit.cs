// Polyfill for C# 9+ `record` / `record class` types compiled against netstandard2.1.
// netstandard2.1 reference assemblies do not declare IsExternalInit; without this
// shim the compiler rejects any positional record (e.g. MapSymbol) on this TFM.
namespace System.Runtime.CompilerServices;

internal static class IsExternalInit
{
}
