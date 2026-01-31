using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SSC.Config;

/// <summary>
/// JSON config file
/// </summary>
/// <typeparam name="t_Type">Type</typeparam>
public abstract class JsonConfig
    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] t_Type>
    where t_Type : JsonConfig<t_Type>, new()
{
    private static t_Type? _instance = null;
    private static readonly string _name = typeof(t_Type).Name;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly string _directoryPath = string.Empty;
    private readonly string _filePath = string.Empty;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    protected JsonSerializerSettings _jsonSerializerSettings = new();
    protected JObject? _rawLoaded = null;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Singleton
    /// </summary>
    public static t_Type Instance
    {
        get
        {
            if (_instance == null)
                _instance = new t_Type();

            return _instance;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    public JsonConfig(string relativePath)
    {
        _directoryPath = Path.GetFullPath(relativePath);
        _filePath = Path.Combine(_directoryPath, $"{_name}.json");

        _jsonSerializerSettings = new JsonSerializerSettings();
        _jsonSerializerSettings.DefaultValueHandling = DefaultValueHandling.Include;
        _jsonSerializerSettings.NullValueHandling = NullValueHandling.Ignore;

        if (!TryCreateFolder())
            Environment.Exit(-1);

        try
        {
            if (File.Exists(_filePath))
            {
                using (var fileStream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        var content = streamReader.ReadToEnd();

                        _rawLoaded = JObject.Parse(content);
                        JsonConvert.PopulateObject(content, this, _jsonSerializerSettings);
                    }
                }

                OnInit(false);
                _rawLoaded = null;
            }
            else
            {
                OnInit(true);
            }

            Save();
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{_name}>.ReadImplementation] Failed to read config file in {_directoryPath}");
            Logging.Log(ELogSeverity.Error, exception);

            TryBackupAndReset();
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Dummy method for warmup
    /// </summary>
    public void Warmup()
    {
        ;
    }
    /// <summary>
    /// Reset config to default
    /// </summary>
    public void Reset()
    {
        if (!TryCreateFolder())
            return;

        try
        {
            var defaultInstance = new t_Type();
            var defaultSerialized = JsonConvert.SerializeObject(defaultInstance, _jsonSerializerSettings);
            JsonConvert.PopulateObject(defaultSerialized, this, _jsonSerializerSettings);

            Save();
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{_name}>.Reset] Failed");
            Logging.Log(ELogSeverity.Error, exception);
        }
    }
    /// <summary>
    /// Save config file
    /// </summary>
    public void Save()
    {
        if (!TryCreateFolder())
            return;

        try
        {
            string data = JsonConvert.SerializeObject(this, Formatting.Indented, _jsonSerializerSettings);
            using (var fileStream = new FileStream(_filePath, FileMode.Create, FileAccess.Write, FileShare.ReadWrite))
            {
                using (var streamWritter = new StreamWriter(fileStream, Encoding.UTF8))
                {
                    streamWritter.WriteLine(data);
                }
            }
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{_name}>.WriteFile] Failed to write file {_filePath}");
            Logging.Log(ELogSeverity.Error, exception);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// On config init
    /// </summary>
    /// <param name="onFileCreate">On file create?</param>
    protected virtual void OnInit(bool onFileCreate)
    {

    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to create the destination folder
    /// </summary>
    /// <returns></returns>
    private bool TryCreateFolder()
    {
        var directory = Path.GetDirectoryName(_filePath);

        try
        {
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory!);

            return true;
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{typeof(t_Type).Name}>.WriteFile] Failed to create directory " + directory!);
            Logging.Log(ELogSeverity.Error, exception);
        }

        return false;
    }
    /// <summary>
    /// Try to backup the existing file and then reset this config
    /// </summary>
    private void TryBackupAndReset()
    {
        if (File.Exists(_filePath))
        {
            try
            {
                File.Move(_filePath,
                    Path.Combine(
                        _directoryPath,
                        Path.GetFileNameWithoutExtension(_filePath) + ".broken_" + Misc.Time.UnixTimeNowMS() + ".json"
                    )
                );
            }
            catch (Exception exception)
            {
                Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{_name}>.WriteFile] Failed to backup file {_filePath}, trying deletion...");
                Logging.Log(ELogSeverity.Error, exception);

                File.Delete(_filePath);
            }
        }

        Reset();
        OnInit(true);
    }
}
