using SoulsFormats;
if (args[0] == "--restore-wall")
{
    var dir = Path.GetFullPath(args[1]);
    var destination = Path.Combine(dir, "corrected");
    Directory.CreateDirectory(destination);
    var stem = "m40_00_ground_011_a";
    var source = File.ReadAllBytes(Path.Combine(dir, "m10_wall_stone.dds"));
    var template = File.ReadAllBytes(Path.Combine(dir, stem + ".dds"));
    if (System.Text.Encoding.ASCII.GetString(source,84,4) != "DXT1" ||
        System.Text.Encoding.ASCII.GetString(template,84,4) != "DX10" ||
        BitConverter.ToInt32(template,128) != 72 ||
        !source.AsSpan(12,12).SequenceEqual(template.AsSpan(12,12)) || BitConverter.ToInt32(source,28) != BitConverter.ToInt32(template,28) ||
        source.Length - 128 != template.Length - 148)
        throw new InvalidDataException("Incompatible BC1 surfaces.");
    var fixedDds = (byte[])template.Clone();
    source.AsSpan(128).CopyTo(fixedDds.AsSpan(148));
    File.WriteAllBytes(Path.Combine(destination, stem + ".dds"),fixedDds);
    var header = args[2];
    var archive = BXF4.Read(File.ReadAllBytes(header), File.ReadAllBytes(Path.ChangeExtension(header, ".tpfbdt")));
    var entry = archive.Files.Single(f => Path.GetFileName(f.Name).Split('.')[0] == stem);
    var tpf = TPF.Read(entry.Bytes.ToArray());
    var texture = tpf.Textures.Single(t => t.Name == stem);
    if (!texture.Bytes.SequenceEqual(template)) throw new InvalidDataException("Target has changed.");
    texture.Bytes = fixedDds;
    entry.Bytes = tpf.Write();
    archive.Write(out byte[] bhd,out byte[] bdt);
    var verified = BXF4.Read(bhd,bdt);
    if (verified.Files.Count != archive.Files.Count || verified.Files.Where((f,i) => f.Name != archive.Files[i].Name || !f.Bytes.Span.SequenceEqual(archive.Files[i].Bytes.Span)).Any()) throw new InvalidDataException("Archive mismatch.");
    var check = verified.Files.Single(f=>f.Name==entry.Name);
    if(!TPF.Read(check.Bytes.ToArray()).Textures.Single(t=>t.Name==stem).Bytes.SequenceEqual(fixedDds)) throw new InvalidDataException("DDS mismatch.");
    File.WriteAllBytes(Path.Combine(destination,Path.GetFileName(header)),bhd);
    File.WriteAllBytes(Path.Combine(destination,Path.GetFileNameWithoutExtension(header)+".tpfbdt"),bdt);
    Console.WriteLine(destination);
    return;
}
var output = Path.GetFullPath(args[0]);
Directory.CreateDirectory(output);
foreach (var folder in args.Skip(1))
{
    bool original = folder.Contains("REMASTERED");
    string target = original ? "m10_wall_stone" : "m40_00_ground_011_a";
    if (File.Exists(Path.Combine(output,target+".dds"))) continue;
    foreach(var header in Directory.GetFiles(folder,"*.tpfbhd"))
    {
        if(!System.Text.Encoding.Unicode.GetString(File.ReadAllBytes(header)).Contains(target,StringComparison.OrdinalIgnoreCase) && !original && !System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(header)).Contains(target,StringComparison.OrdinalIgnoreCase)) continue;
        var data=Path.ChangeExtension(header,".tpfbdt");
        using BinderReader reader = original ? new BXF3Reader(header,data) : new BXF4Reader(header,data);
        foreach(var file in reader.Files.Where(f=>Path.GetFileName(f.Name).Split('.')[0].Equals(target,StringComparison.OrdinalIgnoreCase)))
        {
            var texture=TPF.Read(reader.ReadFile(file).ToArray()).Textures.Single(t=>t.Name.Equals(target,StringComparison.OrdinalIgnoreCase));
            File.WriteAllBytes(Path.Combine(output,target+".dds"),texture.Bytes);
            Console.WriteLine(header+" "+target);
        }
    }
}


