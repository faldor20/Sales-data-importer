namespace Main.DB
open FSharp.Data.Dapper
open Microsoft.Data.Sqlite
open FSharp.Data.Dapper.QuerySeqAsyncBuilder
module Connection=
    let mkMemory()= 
        let a=new SqliteConnection("Data Source=:memory:")
        a

module DB =
    let private connectionF () = Connection.SqliteConnection (Connection.mkMemory())

    let querySeqAsync<'R>          = querySeqAsync<'R> (connectionF)
    let querySingleAsync<'R>       = querySingleAsync<'R> (connectionF)
    let querySingleOptionAsync<'R> = querySingleOptionAsync<'R> (connectionF)
module Schema = 
    let CreateTables = DB.querySingleAsync<int> {
        script """
            DROP TABLE IF EXISTS Customers;
            DROP TABLE IF EXISTS Sales;
            CREATE TABLE Customers(
                Id INTEGER PRIMARY KEY,
                Name VARCHAR(255), 
            );
            CREATE TABLE Sales (
                Id INTEGER PRIMARY KEY,
                CustomerId INTEGER FOREIGN KEY,
               M1 INTEGER NULL,
               M2 INTEGER NULL,
               M3 INTEGER NULL,
               M4 INTEGER NULL,
               M5 INTEGER NULL,
               M6 INTEGER NULL,
               M7 INTEGER NULL,
               M8 INTEGER NULL,
               M9 INTEGER NULL,
               M10 INTEGER NULL,
               M11 INTEGER NULL,
               M12 INTEGER NULL,
            );
        """
    }
module Access=
    type Customer = {
        Id : int64
        Name: string
    }
    type Sales = {
            Id : int64
            CustomerId:int64
            M1:int
            M2:int
            M3:int
            M4:int
            M5:int
            M6:int
            M7:int
            M8:int
            M9:int
            M10:int
            M11:int
            M12:int 
    }
    let New name alias = DB.querySingleAsync<int> {
        script "INSERT INTO Bird (Name, Alias) VALUES (@Name, @Alias)"
        parameters (dict ["Name", box name; "Alias", box alias])
    }