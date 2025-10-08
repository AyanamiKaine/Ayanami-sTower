using System;

namespace StellaLang;

/// <summary>
/// Helper to define simple plain-data structs in VM dictionary memory.
/// Generates words for allocation, field access (get/set) against a base address (pointer).
/// </summary>
public static class StructDefiner
{
    /// <summary>
    /// Describes a struct field with a name and byte offset in the struct layout.
    /// Offsets are from the base address, in bytes.
    /// </summary>
    public sealed record Field
    {
        /// <summary>
        /// Creates a new field description.
        /// </summary>
        /// <param name="Name">Field name (case-insensitive in generated word names).</param>
        /// <param name="OffsetBytes">Byte offset from the struct base address.</param>
        public Field(string Name, int OffsetBytes)
        {
            this.Name = Name;
            this.OffsetBytes = OffsetBytes;
        }

        /// <summary>
        /// The field name.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// The field offset in bytes from the struct base address.
        /// </summary>
        public int OffsetBytes { get; }
    }

    /// <summary>
    /// Defines a struct with 64-bit fields and creates words: NEW-{struct}, GET-{field}, SET-{field}.
    /// All fields use 8-byte slots for simplicity. Caller must ensure VM.Allot/Allocate space is available.
    /// </summary>
    /// <param name="vm">VM to register words into.</param>
    /// <param name="structName">Canonical struct name (e.g., "CIRCLE").</param>
    /// <param name="fields">Fields with their byte offsets (multiples of 8 recommended).</param>
    /// <param name="sizeBytes">Total size of the struct in bytes.</param>
    public static void DefineI64Struct(VMActor vm, string structName, Field[] fields, int sizeBytes)
    {
        // NEW-{struct}: ( ...fieldValuesInOrder -- addr ) allocate and initialize
        var newName = $"NEW-{structName}";
        vm.DefineNative(newName, (Action)(() =>
        {
            int fieldCount = fields.Length;
            if (vm.DataStackCount() < fieldCount)
                throw new InvalidOperationException($"Word '{newName}' requires {fieldCount} field values on stack.");

            // Pop values into array left-to-right
            var vals = new long[fieldCount];
            for (int i = fieldCount - 1; i >= 0; i--)
                vals[i] = vm.PopValue().AsInteger();

            int addr = vm.Allocate(sizeBytes);
            for (int i = 0; i < fieldCount; i++)
                vm.Write64(addr + fields[i].OffsetBytes, vals[i]);
            vm.PushValue(Value.Pointer(addr));
        }));

        // GET-{field}: ( addr -- value )
        foreach (var f in fields)
        {
            var getName = $"GET-{structName}-{f.Name.ToUpper()}";
            vm.DefineNative(getName, (Action)(() =>
            {
                int addr = vm.PopValue().AsPointer();
                vm.PushValue(Value.Integer(vm.Read64(addr + f.OffsetBytes)));
            }));
        }

        // SET-{field}: ( value addr -- )
        foreach (var f in fields)
        {
            var setName = $"SET-{structName}-{f.Name.ToUpper()}";
            vm.DefineNative(setName, (Action)(() =>
            {
                int addr = vm.PopValue().AsPointer();
                long val = vm.PopValue().AsInteger();
                vm.Write64(addr + f.OffsetBytes, val);
            }));
        }
    }
}
