using Newtonsoft.Json.Linq;
using OpenGM.IO;

namespace OpenGM.VirtualMachine.BuiltInFunctions;

public static class FileFunctions
{
    private static readonly Dictionary<int, FileHandle> _fileHandles = new(32);
    private static IniFile? _iniFile;

    // file_bin_open
    // file_bin_rewrite
    // file_bin_close
    // file_bin_position
    // file_bin_size
    // file_bin_seek
    // file_bin_read_byte
    // file_bin_write_byte
    // file_text_open_from_string

    [GMLFunction("file_text_open_read")]
    public static object? file_text_open_read(object?[] args)
    {
        var fname = args[0].Conv<string>();
        var filepath = Path.Combine(Entry.DataWinFolder, fname);

        DebugLog.Log($"file_text_open_read {filepath}");

        if (!File.Exists(filepath))
        {
            return -1;
        }

        var fileStream = new FileStream(filepath, FileMode.Open, FileAccess.Read);

        if (_fileHandles.Count == 32)
        {
            fileStream.Close();
            return -1;
        }

        var highestIndex = -1;
        if (_fileHandles.Count > 0)
        {
            highestIndex = _fileHandles.Keys.Max();
        }

        var handle = new FileHandle
        {
            Reader = new StreamReader(fileStream)
        };

        _fileHandles.Add(highestIndex + 1, handle);
        return highestIndex + 1;
    }

    [GMLFunction("file_text_open_write")]
    public static object? file_text_open_write(object?[] args)
    {
        if (_fileHandles.Count == 32)
        {
            return -1;
        }

        var fname = args[0].Conv<string>();
        var filepath = Path.Combine(Entry.DataWinFolder, fname);

        File.Delete(filepath);
        var fileStream = new FileStream(filepath, FileMode.Create, FileAccess.Write);

        var highestIndex = -1;
        if (_fileHandles.Count > 0)
        {
            highestIndex = _fileHandles.Keys.Max();
        }

        var handle = new FileHandle
        {
            Writer = new StreamWriter(fileStream)
        };

        _fileHandles.Add(highestIndex + 1, handle);
        return highestIndex + 1;
    }

    // file_text_open_append

    [GMLFunction("file_text_close")]
    public static object? file_text_close(object?[] args)
    {
        var index = args[0].Conv<int>();

        if (_fileHandles.ContainsKey(index))
        {
            if (_fileHandles[index].Reader != null)
            {
                _fileHandles[index].Reader!.Close();
                _fileHandles[index].Reader!.Dispose();
            }

            if (_fileHandles[index].Writer != null)
            {
                _fileHandles[index].Writer!.Close();
                _fileHandles[index].Writer!.Dispose();
            }

            _fileHandles.Remove(index);
        }

        return null;
    }

    [GMLFunction("file_text_read_string")]
    public static object file_text_read_string(object?[] args)
    {
        var fileid = args[0].Conv<int>();
        var reader = _fileHandles[fileid].Reader!;

        var result = "";
        while (reader.Peek() != 0x0D && reader.Peek() >= 0)
        {
            result += (char)reader.Read();
        }

        return result;
    }

    [GMLFunction("file_text_read_real")]
    public static object file_text_read_real(object?[] args)
    {
        var fileid = args[0].Conv<int>();
        var reader = _fileHandles[fileid].Reader!;

        var result = "";
        while (reader.Peek() != 0x0D && reader.Peek() >= 0)
        {
            result += (char)reader.Read();
        }

        return double.Parse(result);
    }

    [GMLFunction("file_text_readln")]
    public static object? file_text_readln(object?[] args)
    {
        var fileid = args[0].Conv<int>();
        var reader = _fileHandles[fileid].Reader!;
        return reader.ReadLine(); // BUG: returns null if eof
    }

    [GMLFunction("file_text_eof")]
    public static object file_text_eof(object?[] args)
    {
        var fileid = args[0].Conv<int>();
        var reader = _fileHandles[fileid].Reader!;
        return reader.EndOfStream;
    }

    // file_text_eoln

    [GMLFunction("file_text_write_string")]
    public static object? file_text_write_string(object?[] args)
    {
        var fileid = args[0].Conv<int>();
        var str = args[1].Conv<string>();
        var writer = _fileHandles[fileid].Writer!;
        writer.Write(str);
        return null;
    }

    [GMLFunction("file_text_write_real")]
    public static object? file_text_write_real(object?[] args)
    {
        var fileid = args[0].Conv<int>();
        var val = args[1];
        var writer = _fileHandles[fileid].Writer!;

        if (val is not int and not double and not float and not long and not short)
        {
            DebugLog.LogError($"file_text_write_real got {val} ({val!.GetType()}) instead of a real!");
            // i have no fucking idea
            writer.Write(0);
            return null;
        }

        writer.Write(val.Conv<double>());
        return null;
    }

    [GMLFunction("file_text_writeln")]
    public static object? file_text_writeln(object?[] args)
    {
        var fileid = args[0].Conv<int>();
        var writer = _fileHandles[fileid].Writer!;
        writer.WriteLine();
        return null;
    }

    [GMLFunction("file_exists")]
    public static object file_exists(object?[] args)
    {
        var fname = args[0].Conv<string>();
        var filepath = Path.Combine(Entry.DataWinFolder, fname);
        return File.Exists(filepath);
    }

    [GMLFunction("file_delete")]
    public static object file_delete(object?[] args)
    {
        var fname = args[0].Conv<string>();
        var filepath = Path.Combine(Entry.DataWinFolder, fname);
        File.Delete(filepath);
        return true; // TODO : this should return false if this fails.
    }

    // file_rename

    [GMLFunction("file_copy")]
    public static object? file_copy(object?[] args)
    {
        var fname = args[0].Conv<string>();
        var newname = args[1].Conv<string>();

        fname = Path.Combine(Entry.DataWinFolder, fname);
        newname = Path.Combine(Entry.DataWinFolder, newname);

        if (File.Exists(newname))
        {
            throw new Exception("File already exists.");
        }

        File.Copy(fname, newname);

        return null;
    }

    // directory_exists
    // directory_create
    // directory_destroy
    
    [GMLFunction("file_find_first", GMLFunctionFlags.Stub)]
    public static object? file_find_first(object?[] args)
    {
        return "";
    }

    [GMLFunction("file_find_next", GMLFunctionFlags.Stub)]
    public static object? file_find_next(object?[] args)
    {
        return "";
    }

    [GMLFunction("file_find_close", GMLFunctionFlags.Stub)]
    public static object? file_find_close(object?[] args)
    {
        return null;
    }

    // file_attributes
    // filename_name
    // filename_path
    // filename_dir
    // filename_drive
    // filename_ext
    // filename_change_ext

    [GMLFunction("parameter_count")]
    public static object? parameter_count(object?[] args)
    {
        return Entry.LaunchParameters.Length;
    }

    [GMLFunction("parameter_string")]
    public static object? parameter_string(object?[] args)
    {
        var n = args[0].Conv<int>();
        return Entry.LaunchParameters[n - 1];
    }

    [GMLFunction("environment_get_variable")]
    public static object? environment_get_variable(object?[] args)
    {
        var name = args[0].Conv<string>();
        // TODO : is this right? idk
        return Environment.GetEnvironmentVariable(name);
    }

    // ini_open_from_string

    [GMLFunction("ini_open")]
    public static object? ini_open(object?[] args)
    {
        var name = args[0].Conv<string>();

        if (_iniFile != null)
        {
            // Docs say this throws an error.
            // C++ and HTML runners just save the old ini file and open the new one, with no error.
            // I love Gamemaker.

            ini_close(new object[0]);
        }

        var filepath = Path.Combine(Entry.DataWinFolder, name);

        if (!File.Exists(filepath))
        {
            _iniFile = new IniFile { Name = name };
            return null;
        }

        var lines = File.ReadAllLines(filepath);

        KeyValuePair<string, string> ParseKeyValue(string line)
        {
            var lineByEquals = line.Split('=');
            var key = lineByEquals[0].Trim();
            var value = lineByEquals[1].Trim();
            value = value.Trim('"');
            return new KeyValuePair<string, string>(key, value);
        }

        _iniFile = new IniFile { Name = name };
        IniSection? currentSection = null;

        for (var i = 0; i < lines.Length; i++)
        {
            var currentLine = lines[i];
            if (currentLine.StartsWith('[') && currentLine.EndsWith("]"))
            {
                currentSection = new IniSection(currentLine.TrimStart('[').TrimEnd(']'));
                _iniFile.Sections.Add(currentSection);
                continue;
            }

            if (string.IsNullOrEmpty(currentLine))
            {
                continue;
            }

            var keyvalue = ParseKeyValue(currentLine);
            currentSection?.Dict.Add(keyvalue.Key, keyvalue.Value);
        }

        return null;
    }

    [GMLFunction("ini_close")]
    public static object? ini_close(object?[] args)
    {
        if (_iniFile == null)
        {
            return null;
        }

        var filepath = Path.Combine(Entry.DataWinFolder, _iniFile!.Name);
        File.Delete(filepath);
        var fileStream = new FileStream(filepath, FileMode.Append, FileAccess.Write);
        var streamWriter = new StreamWriter(fileStream);

        foreach (var section in _iniFile.Sections)
        {
            streamWriter.WriteLine($"[{section.Name}]");
            foreach (var kv in section.Dict)
            {
                streamWriter.WriteLine($"{kv.Key}=\"{kv.Value}\"");
            }
        }

        var text = streamWriter.ToString();

        streamWriter.Close();
        streamWriter.Dispose();
        _iniFile = null;

        return text; // BUG: this does NOT return the written text
    }

    [GMLFunction("ini_read_string")]
    public static object ini_read_string(object?[] args)
    {
        var section = args[0].Conv<string>();
        var key = args[1].Conv<string>();
        var value = args[2].Conv<string>();

        var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

        if (sectionClass == null)
        {
            return value;
        }

        if (!sectionClass.Dict.ContainsKey(key))
        {
            return value;
        }

        return sectionClass.Dict[key];
    }

    [GMLFunction("ini_read_real")]
    public static object? ini_read_real(object?[] args)
    {
        var section = args[0].Conv<string>();
        var key = args[1].Conv<string>();
        var value = args[2].Conv<double>();

        var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

        if (sectionClass == null)
        {
            return value;
        }

        if (!sectionClass.Dict.ContainsKey(key))
        {
            return value;
        }

        if (!double.TryParse(sectionClass.Dict[key], out var _res))
        {
            // TODO : check what it does here. maybe it only parses up to an invalid character?
            return value;
        }

        return _res;
    }

    [GMLFunction("ini_write_string")]
    public static object? ini_write_string(object?[] args)
    {
        var section = args[0].Conv<string>();
        var key = args[1].Conv<string>();
        var value = args[2].Conv<string>();

        var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

        if (sectionClass == null)
        {
            sectionClass = new IniSection(section);
            _iniFile.Sections.Add(sectionClass);
        }

        sectionClass.Dict[key] = value;

        return null;
    }

    [GMLFunction("ini_write_real")]
    public static object? ini_write_real(object?[] args)
    {
        var section = args[0].Conv<string>();
        var key = args[1].Conv<string>();
        var value = args[2].Conv<double>();

        var sectionClass = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

        if (sectionClass == null)
        {
            sectionClass = new IniSection(section);
            _iniFile.Sections.Add(sectionClass);
        }

        sectionClass.Dict[key] = value.ToString();

        return null;
    }

    [GMLFunction("ini_key_exists")]
    public static object? ini_key_exists(object?[] args)
    {
        var section = args[0].Conv<string>();
        var key = args[1].Conv<string>();

        var sec = _iniFile!.Sections.FirstOrDefault(x => x.Name == section);

        return sec != null && sec.Dict.ContainsKey(key);
    }

    [GMLFunction("ini_section_exists")]
    public static object? ini_section_exists(object?[] args)
    {
        var section = args[0].Conv<string>();
        return _iniFile!.Sections.Any(x => x.Name == section);
    }

    // ini_key_delete
    // ini_section_delete
    // http_get
    // http_get_file
    // http_request
    // http_get_request_crossorigin
    // http_set_request_crossorigin
    // json_encode

    [GMLFunction("json_decode")]
    public static object? json_decode(object?[] args)
    {
        // is recursive weeeeeeeeeeee
        static object Parse(JToken jToken)
        {
            switch (jToken)
            {
                case JValue jValue:
                    return jValue.Value!;
                case JArray jArray:
                {
                    var dsList = (int)DataStructuresFunctions.ds_list_create();
                    foreach (var item in jArray)
                    {
                            // TODO: make and call the proper function for maps and lists
                            DataStructuresFunctions.ds_list_add(dsList, Parse(item));
                    }
                    return dsList;
                }
                case JObject jObject:
                {
                    var dsMap = (int)DataStructuresFunctions.ds_map_create();
                    foreach (var (name, value) in jObject)
                    {
                        // TODO: make and call the proper function for maps and lists
                        DataStructuresFunctions.ds_map_add(dsMap, name, Parse(value!));
                    }
                    return dsMap;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        var @string = args[0].Conv<string>();
        var jToken = JToken.Parse(@string);

        switch (jToken)
        {
            case JValue jValue:
            {
                var dsMap = (int)DataStructuresFunctions.ds_map_create();
                DataStructuresFunctions.ds_map_add(dsMap, "default", Parse(jValue));
                return dsMap;
            }
            case JArray jArray:
            {
                var dsMap = (int)DataStructuresFunctions.ds_map_create();
                DataStructuresFunctions.ds_map_add(dsMap, "default", Parse(jArray));
                return dsMap;
            }
            case JObject jObject:
            {
                return Parse(jObject);
            }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // json_stringify
    // json_parse
    // zip_unzip
    // load_csv
}

public class FileHandle
{
    public StreamReader? Reader;
    public StreamWriter? Writer;
}