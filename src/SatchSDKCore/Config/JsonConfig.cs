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
/// <typeparam name="TConfigType">Type</typeparam>
public abstract class JsonConfig
    <[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)] TConfigType>
    where TConfigType : JsonConfig<TConfigType>, new()
{
    private static TConfigType? s_Instance;
    private static readonly string s_Name = typeof(TConfigType).Name;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    private readonly string _directoryPath = string.Empty;
    private readonly string _filePath = string.Empty;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    protected JsonSerializerSettings _jsonSerializerSettings = new();
    protected JObject? _rawLoaded;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Singleton
    /// </summary>
    public static TConfigType Instance
    {
        get
        {
            if (s_Instance == null)
                s_Instance = new TConfigType();

            return s_Instance;
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
        _filePath = Path.Combine(_directoryPath, $"{s_Name}.json");

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
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{s_Name}>.ReadImplementation] Failed to read config file in {_directoryPath}");
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
            var defaultInstance = new TConfigType();
            var defaultSerialized = JsonConvert.SerializeObject(defaultInstance, _jsonSerializerSettings);
            JsonConvert.PopulateObject(defaultSerialized, this, _jsonSerializerSettings);

            Save();
        }
        catch (Exception exception)
        {
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{s_Name}>.Reset] Failed");
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
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{s_Name}>.WriteFile] Failed to write file {_filePath}");
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
            Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{typeof(TConfigType).Name}>.WriteFile] Failed to create directory " + directory!);
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
                Logging.Log(ELogSeverity.Error, $"[Config][JSONConfig<{s_Name}>.WriteFile] Failed to backup file {_filePath}, trying deletion...");
                Logging.Log(ELogSeverity.Error, exception);

                File.Delete(_filePath);
            }
        }

        Reset();
        OnInit(true);
    }
}
