namespace BoringWebApp.Test
open System
open System.Data
open System.Data.Common

/// Simulates nested transactions in tests by no-op in Commit, and fail in Rollback
type TestTransaction(db: DbConnection, il: IsolationLevel) =
    inherit DbTransaction()
    override this.DbConnection = db
    override this.IsolationLevel = il
    override this.Commit() = ()
    override this.Rollback() = raise <| NotSupportedException()


/// A wrapper type for IDbConnection that is intended to allow each test to run in an isolated
/// transaction which is rolled back between tests.
type TestDbConnection(db: DbConnection) =
    inherit DbConnection()
    override this.ConnectionString
        with get() = db.ConnectionString and
             set _ = raise <| NotSupportedException()

    override this.ConnectionTimeout = db.ConnectionTimeout
    override this.Database = db.Database
    override this.DataSource = db.DataSource
    override this.ServerVersion = db.ServerVersion
    override this.State = db.State
    override this.BeginDbTransaction(il: IsolationLevel) = new TestTransaction(this, il) :> DbTransaction
    override this.CreateDbCommand() = db.CreateCommand()
    override this.Open() = raise <| NotSupportedException()
    override this.Close() = raise <| NotSupportedException()
    override this.ChangeDatabase(_databaseName: string) = raise <| NotSupportedException()

