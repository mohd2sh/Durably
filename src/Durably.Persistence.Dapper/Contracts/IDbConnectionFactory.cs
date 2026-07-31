using System.Data.Common;

namespace Durably;

/// <summary>Creates new, unopened ADO.NET connections for the store. One per operation.</summary>
internal interface IDbConnectionFactory
{
    DbConnection Create();
}
