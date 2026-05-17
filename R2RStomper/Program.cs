using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.Writer;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: R2RStomper <input.dll> <output.dll>");
    return 1;
}

string inputPath  = args[0];
string outputPath = args[1];

Console.WriteLine($"[*] Loading: {inputPath}");
var module = ModuleDefMD.Load(inputPath);

// Locate KeyA field and the two hidden helper methods
FieldDef? keyAField = null;
MethodDef? getRealKeyMethod = null;
MethodDef? writeHiddenMethod = null;

foreach (var type in module.GetTypes())
{
    foreach (var field in type.Fields)
        if (field.Name == "KeyA") keyAField = field;

    foreach (var method in type.Methods)
    {
        if (method.Name == "GetRealKey")       getRealKeyMethod = method;
        if (method.Name == "WriteHiddenMessage") writeHiddenMethod = method;
    }
}

if (keyAField is null)
{
    Console.Error.WriteLine("[-] KeyA field not found.");
    return 1;
}
if (getRealKeyMethod is null || writeHiddenMethod is null)
{
    Console.Error.WriteLine($"[-] Hidden methods not found — " +
        $"GetRealKey={getRealKeyMethod is not null}, WriteHiddenMessage={writeHiddenMethod is not null}");
    Console.Error.WriteLine("    Build with -p:UseRealKey=true to include hidden methods.");
    return 1;
}

Console.WriteLine($"[+] KeyA field    : {keyAField.FullName}");
Console.WriteLine($"[+] GetRealKey    : {getRealKeyMethod.FullName}");
Console.WriteLine($"[+] WriteHidden   : {writeHiddenMethod.FullName}");

// ── Patch Main ──────────────────────────────────────────────────────────────
// Remove: call void WriteHiddenMessage()
// Replace: call byte[] GetRealKey()  →  ldsfld KeyA
bool removedHidden = false, patchedKey = false;

foreach (var type in module.GetTypes())
{
    foreach (var method in type.Methods)
    {
        if (method.Name != "Main" || !method.HasBody) continue;

        var instrs = method.Body.Instructions;
        for (int i = instrs.Count - 1; i >= 0; i--)
        {
            var instr = instrs[i];
            if (instr.OpCode != OpCodes.Call) continue;
            if (instr.Operand is not IMethod m) continue;

            if (m.Name == "WriteHiddenMessage")
            {
                instrs.RemoveAt(i);
                removedHidden = true;
                Console.WriteLine($"[+] Removed  call WriteHiddenMessage() from {method.FullName}");
            }
            else if (m.Name == "GetRealKey")
            {
                instr.OpCode  = OpCodes.Ldsfld;
                instr.Operand = keyAField;
                patchedKey = true;
                Console.WriteLine($"[+] Patched  call GetRealKey() → ldsfld KeyA in {method.FullName}");
            }
        }

        if (removedHidden && patchedKey) break;
    }
    if (removedHidden && patchedKey) break;
}

if (!removedHidden || !patchedKey)
{
    Console.Error.WriteLine(
        $"[-] Patch incomplete — WriteHiddenMessage removed={removedHidden}, GetRealKey patched={patchedKey}");
    return 1;
}

// ── Gut the helper methods' IL bodies ────────────────────────────────────────
// Native R2R code has already inlined them into Main; the IL bodies are dead.
// Replacing them with minimal stubs prevents KEY_B bytes from appearing in
// any IL-level decompiler output.

GutVoidMethod(writeHiddenMethod);
Console.WriteLine($"[+] Gutted IL of WriteHiddenMessage (native R2R body preserved)");

GutReturnNullMethod(getRealKeyMethod);
Console.WriteLine($"[+] Gutted IL of GetRealKey         (native R2R body preserved)");

// ── Write stomped DLL ────────────────────────────────────────────────────────
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);

var opts = new NativeModuleWriterOptions(module, optimizeImageSize: false);
opts.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
module.NativeWrite(outputPath, opts);

Console.WriteLine($"[+] Stomped DLL written: {outputPath}");
Console.WriteLine($"    IL  (dnSpy) : Main uses KeyA, helper methods are empty stubs");
Console.WriteLine($"    R2R native  : Main prints 'This is hidden message', encrypts with KEY_B");
return 0;

// ── Helpers ──────────────────────────────────────────────────────────────────

static void GutVoidMethod(MethodDef m)
{
    m.Body.Instructions.Clear();
    m.Body.ExceptionHandlers.Clear();
    m.Body.Variables.Clear();
    m.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
}

static void GutReturnNullMethod(MethodDef m)
{
    m.Body.Instructions.Clear();
    m.Body.ExceptionHandlers.Clear();
    m.Body.Variables.Clear();
    m.Body.Instructions.Add(OpCodes.Ldnull.ToInstruction());
    m.Body.Instructions.Add(OpCodes.Ret.ToInstruction());
}
