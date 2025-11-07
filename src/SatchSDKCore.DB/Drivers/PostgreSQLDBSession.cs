using Npgsql;
using System;
using System.Data.Common;

namespace SSC.DB.Drivers;

/// <summary>
/// PostgreSQL DBSession implementation
/// </summary>
internal class PostgreSQLDBSession : DBSession
{
    private             object                  m_Lock                      = new object();
    private             PostgreSQLDBInstance    m_Instance                  = null!;
    private             string                  m_ConnectionString          = string.Empty;
    private             string                  m_LogIdentifier             = string.Empty;
    private             NpgsqlConnection?       m_PostgreSQLDBConnection    = null;
    private volatile    bool                    m_InUse                     = false;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override DBInstance DBInstance => m_Instance;

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="PostgreSQLDBInstance">Parent PostgreSQLDBInstance</param>
    /// <param name="connectionString">Connection string for the driver</param>
    internal PostgreSQLDBSession(PostgreSQLDBInstance PostgreSQLDBInstance, string connectionString, string logIdentifier)
    {
        m_Instance          = PostgreSQLDBInstance;
        m_ConnectionString  = connectionString;
        m_LogIdentifier     = logIdentifier;

        OpenDBConnection();

        /*var ee = new PostgreSQLCommand("insert into oc_accounts(uid, data) VALUES (@arg1, @arg2)", m_PostgreSQLDBConnection);
        ee.Parameters.Add(new PostgreSQLParameter(){ ParameterName = "@uid"});
        ee.Parameters.Add(new PostgreSQLParameter(){ ParameterName = "@data" });
        ee.Prepare();

        var reader = new PostgreSQLCommand("SELECT\r\n    OWNER_OBJECT_TYPE, OWNER_OBJECT_SCHEMA, OWNER_OBJECT_NAME,\r\n    STATEMENT_NAME, SQL_TEXT\r\nFROM performance_schema.`prepared_statements_instances`;", m_PostgreSQLDBConnection).ExecuteReader();
        while (reader.ReadSocket())
        {
            System.Console.WriteLine(reader.GetValue(0));
            System.Console.WriteLine(reader.GetValue(1));
        }*/
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Return this session into the usable pool for later uses
    /// </summary>
    /// <param name="force">Force the dispose</param>
    public override void Dispose(bool force = false)
    {
        m_Instance.ReleaseSession(this);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Create a DbCommand for the driver
    /// </summary>
    /// <returns>New DbCommand</returns>
    public override DbCommand CreateDbCommand()
    {
        return new NpgsqlCommand(null, m_PostgreSQLDBConnection, null);
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    public override void Commit()
    {
        throw new NotImplementedException();
    }

    public override void Rollback()
    {
        throw new NotImplementedException();
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Try to acquire this DBSession
    /// </summary>
    /// <returns>True if acquired</returns>
    internal bool TryAcquire()
    {
        if (m_InUse)
            return false;

        lock (m_Lock)
        {
            if (m_InUse)
                return false;

            try
            {
                switch (m_PostgreSQLDBConnection?.State)
                {
                    case System.Data.ConnectionState.Closed:
                    case System.Data.ConnectionState.Broken:
                        OpenDBConnection();
                        break;

                }
            }
            catch(Exception) { }

            if (m_PostgreSQLDBConnection?.State != System.Data.ConnectionState.Open)
                return false;

            m_InUse = true;
        }

        return true;
    }
    /// <summary>
    /// Release from a TryAcquire
    /// </summary>
    internal void Release()
    {
        if (m_InUse)
            return;

        while (m_PostgreSQLDBConnection!.State == System.Data.ConnectionState.Fetching
            || m_PostgreSQLDBConnection!.State == System.Data.ConnectionState.Executing)
            System.Threading.Thread.Yield();

        lock (m_Lock)
            m_InUse = false;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Open a DB Connection
    /// </summary>
    /// <param name="connection">Connection to open</param>
    /// <exception cref="Exception"></exception>
    private void OpenDBConnection()
    {
        try
        {
            if (m_PostgreSQLDBConnection != null)
            {
                try { m_PostgreSQLDBConnection.Close(); }
                catch (Exception) { }
            }

            m_PostgreSQLDBConnection = new NpgsqlConnection(m_ConnectionString);
            m_PostgreSQLDBConnection.Open();
        }
        catch (Exception l_Exception)
        {
            Logging.Log(ELogSeverity.Error, $"[Database][PostgreSQLDBSession.OpenDBConnection<{m_LogIdentifier}>] Can not connect to the database server");
            Logging.Log(ELogSeverity.Error, l_Exception);

            throw new Exception($"[Database][PostgreSQLDBSession.OpenDBConnection<{m_LogIdentifier}>] Can not connect to the database server");
        }
    }
}
