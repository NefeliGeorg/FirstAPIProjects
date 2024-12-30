using System;
using System.Web;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text;
using VI.Base;
using VI.DB;
using VI.DB.Entities;
using QBM.CompositionApi.ApiManager;
using QBM.CompositionApi.Definition;
using QBM.CompositionApi.PlugIns;
using QBM.CompositionApi.Crud;
using QER.CompositionApi.Portal;
using QBM.CompositionApi.Handling;
using NLog.Targets;
using System.Xml.Linq;
using VI.DB.DataAccess;
using VI.DB.Sync;

// Creating a
// Conflict

namespace QBM.CompositionApi
{
    public class CustomMethod : IApiProviderFor<QER.CompositionApi.Portal.PortalApiProject>, IApiProvider
    {
        public void Build(IApiBuilder builder)
        {
            // Add a GET method named "example/predefinedsqlselectedcolumns" to the API
            builder.AddMethod(Method.Define("example/predefinedsqlselectedcolumns")
                  .Handle<PostedPreSQLParams, List<List<ColumnData>>>("POST", async (posted, qr, ct) =>
                  {
                      // Initialize a list to hold the results, where each result is a list of ColumnData objects
                      var results = new List<List<ColumnData>>();

                      // Resolve an instance of IStatementRunner from the session
                      var runner = qr.Session.Resolve<IStatementRunner>();

                      // Execute the predefined SQL statement that was given
                      using (var reader = runner.SqlExecute(posted.IdentQBMLimitedSQL, new[]
                      {
                          // Create a query parameter named "department" with the value that was given
                          QueryParameter.Create("uiddepartment", posted.Department),

                          // Create a query parameter named "employeetype" with the value that was given
                          QueryParameter.Create("employeetype", posted.EmployeeType),

                          // Create a query parameter named "description" with the value that was given
                          QueryParameter.Create("description", posted.Description),

                          // Create a query parameter named "identitytype" with the value that was given
                          QueryParameter.Create("identitytype", posted.IdentityType)
                      }))
                      {
                          // Read each row returned by the SQL query
                          while (reader.Read())
                          {
                              // Create a list of ColumnData objects for each column in the row
                              var row = new List<ColumnData>
                              {
                                  // Map the "UID_Person" column
                                  new ColumnData
                                  {
                                      Column = "User's ID",
                                      Value = reader["UID_Person"].ToString()
                                  },
                                  // Map the "FirstName" column
                                  new ColumnData
                                  {
                                      Column = "User's First Name",
                                      Value = reader["FirstName"].ToString()
                                  },
                                  // Map the "LastName" column
                                  new ColumnData
                                  {
                                      Column = "User's Last Name",
                                      Value = reader["LastName"].ToString()
                                  }
                              };

                              // Add the row to the results list
                              results.Add(row);
                          }
                      }

                      // Return the results as an array
                      return results;
                  }));
        


            builder.AddMethod(Method.Define("example/getgroups")
                .Handle<PostedKeyEx4, List<object>>("POST", async (posted, qr, ct) =>
                {
                    // Build a query to select the fields from the "AADGroup" table
                    // For the groups (records) that match the filters "posted"
                    var query = Query.From("AADGroup")
                        .Select("*")
                        .Where(string.Format(@"UID_AADOrganization = '{0}' AND IsMailEnabled = '{1}'", posted.UID_AADOrganization, posted.IsMailEnabled));
                    
                    // Retrieve the entities matching the query asynchronously
                    var getCollection = await qr.Session.Source()
                        .GetCollectionAsync(query, EntityCollectionLoadType.Default)
                        .ConfigureAwait(false);

                    // Initialize an object list to hold the response data
                    List<object> entityArray = new List<object>();

                    // Iterate the getCollection and fill the object list
                    foreach (var entity in getCollection)
                    {
                        entityArray.Add(await ReturnedFields.fromEntity(entity, qr.Session)
                            .ConfigureAwait(false));
                    }
                    return entityArray;
                }));



            builder.AddMethod(Method.Define("example/getgroup")
                .Handle<PostedKey, ReturnedFields>("POST", async (posted, qr, ct) =>
                {
                    // Build a query to select the fields from the "AADGroup" table
                    // where the UID_AADGroup matches the "posted" value from the user
                    /*var query = Query.From("AADGroup")
                        .Select("*")
                        .Where(string.Format(@"UID_AADGroup IN (
                            SELECT UID_AADGroup FROM AADGroup WHERE UID_AADGroup = '{0}'
                        )", posted.UID_AADGroup));*/

                    var query = Query.From("AADGroup")
                        .Select("*")
                        .Where(string.Format(@"UID_AADGroup = '{0}'", posted.UID_AADGroup));

                    // Attempt to retrieve the entity matching the query asynchronously
                    var tryGet = await qr.Session.Source()
                        .TryGetAsync(query, EntityLoadType.DelayedLogic)
                        .ConfigureAwait(false);

                    // Convert the retrieved entity to a ReturnedName object and return it
                    return await ReturnedFields.fromEntity(tryGet.Result, qr.Session)
                        .ConfigureAwait(false);
                }));



            /*builder.AddMethod(Method.Define("example/addgroupmember")
                .Handle<PostedKeyEx42>("POST", async (posted, qr, ct) =>
                {
                    // Build a query to insert the appropriate fields to the "PersonWantsOrg" table
                    var query = Query.Insert("AADGroup")
                        .Select("*")
                        .Where(string.Format(@"UID_AADGroup = '{0}'", posted.UID_AADGroup));

                    // Attempt to retrieve the entity matching the query asynchronously
                    var tryGet = await qr.Session.Source()
                        .TryGetAsync(query, EntityLoadType.DelayedLogic)
                        .ConfigureAwait(false);

                    // Convert the retrieved entity to a ReturnedName object and return it
                    return await ReturnedFields.fromEntity(tryGet.Result, qr.Session)
                        .ConfigureAwait(false);

                }));*/



            builder.AddMethod(Method.Define("example/insertgroup")
                .Handle<PostedID>("POST", async (posted, qr, ct) =>
                {
                    // Variables to hold column data from the posted request
                    string displayName = "";
                    string uidaadOrganization = "";
                    string mailNickName = "";
                    string unsDisplay = "";
                    string uidaccProduct = "";
                    var uidperson = qr.Session.User().Uid;  // Gets the UID of the current user

                    // Loop through each column in the posted data to extract values
                    foreach (var column in posted.columns)
                    {
                        // Check each column name and assign its value to the corresponding variable
                        if (column.column == "DisplayName")
                        {
                            displayName = column.value;
                        }

                        if (column.column == "UID_AADOrganization")
                        {
                            uidaadOrganization = column.value;
                        }

                        if (column.column == "MailNickName")
                        {
                            mailNickName = column.value;
                        }

                        if (column.column == "UNSDisplay")
                        {
                            unsDisplay = column.value;
                        }

                        if (column.column == "UID_AccProduct")
                        {
                            uidaccProduct = column.value;
                        }
                    }

                    if (displayName.StartsWith("AAD")) {
                        // Create a new AAGroup entity
                        var newID = await qr.Session.Source().CreateNewAsync("AADGroup",
                            new EntityParameters
                            {
                                CreationType = EntityCreationType.DelayedLogic
                            }, ct).ConfigureAwait(false);

                        // Set the values for the new Group entity
                        await newID.PutValueAsync("DisplayName", displayName, ct).ConfigureAwait(false);
                        await newID.PutValueAsync("UID_AADOrganization", uidaadOrganization, ct).ConfigureAwait(false);
                        await newID.PutValueAsync("MailNickName", mailNickName, ct).ConfigureAwait(false);
                        await newID.PutValueAsync("UNSDisplay", unsDisplay, ct).ConfigureAwait(false);
                        await newID.PutValueAsync("UID_AccProduct", uidaccProduct, ct).ConfigureAwait(false);

                        // Start Unit of Work to save the new entity to the database
                        using (var u = qr.Session.StartUnitOfWork())
                        {
                            await u.PutAsync(newID, ct).ConfigureAwait(false);  // Add the new entity to the unit of work
                            await u.CommitAsync(ct).ConfigureAwait(false);  // Commit the transaction to persist changes
                        }
                    } else
                    {
                        throw new HttpException( 600,"The DisplayName must always start with the AAD");
                    }
                }));



            builder.AddMethod(Method.Define("example/updategroup")
                .Handle<PostedID_>("POST", async (posted, qr, ct) =>
                {
                    string displayName = "";
                    string uidaadOrganization = "";
                    string mailNickName = "";
                    string unsDisplay = "";
                    string uidaccProduct = "";
                    string objectkey = "";

                    // Extract the group uid from the posted data
                    foreach (var column in posted.element)
                    {
                        if (column.column == "UID_AADGroup")
                        {
                            // Get the UID_AADGroup of the entity to be updated
                            objectkey = column.value.ToString();
                        }
                    }

                    // Build a query to select all columns from the "AADGroup" table where UID_AADGroup matches
                    var query1 = Query.From("AADGroup")
                                      .Select("*")
                                      .Where(string.Format("UID_AADGroup = '{0}'", objectkey));

                    // Attempt to retrieve the entity asynchronously
                    var tryget = await qr.Session.Source()
                                       .TryGetAsync(query1, EntityLoadType.DelayedLogic, ct)
                                       .ConfigureAwait(false);

                    // Check if the entity was successfully retrieved
                    if (tryget.Success)
                    {
                        // Loop through each column in the posted data to update the entity's properties
                        foreach (var column in posted.columns)
                        {
                            // Assign values based on column names and update the entity accordingly
                            if (column.column == "DisplayName")
                            {
                                displayName = column.value.ToString();
                                await tryget.Result.PutValueAsync("DisplayName", displayName, ct).ConfigureAwait(false);
                            }
                            else if (column.column == "UID_AADOrganization")
                            {
                                uidaadOrganization = column.value.ToString();
                                await tryget.Result.PutValueAsync("UID_AADOrganization", uidaadOrganization, ct).ConfigureAwait(false);
                            }
                            else if (column.column == "MailNickName")
                            {
                                mailNickName = column.value.ToString();
                                await tryget.Result.PutValueAsync("MailNickName", mailNickName, ct).ConfigureAwait(false);
                            }
                            else if (column.column == "UNSDisplay")
                            {
                                unsDisplay = column.value.ToString();
                                await tryget.Result.PutValueAsync("UNSDisplay", unsDisplay, ct).ConfigureAwait(false);
                            }
                            else if (column.column == "UID_AccProduct")
                            {
                                uidaccProduct = column.value.ToString();
                                await tryget.Result.PutValueAsync("UID_AccProduct", uidaccProduct, ct).ConfigureAwait(false);
                            }
                        }

                        // Start a unit of work to save changes to the database
                        using (var u = qr.Session.StartUnitOfWork())
                        {
                            // Add the updated entity to the unit of work
                            await u.PutAsync(tryget.Result, ct).ConfigureAwait(false);

                            // Commit the unit of work to persist changes
                            await u.CommitAsync(ct).ConfigureAwait(false);
                        }
                    }
                }));


            builder.AddMethod(Method.Define("example/deletegroup")
                .Handle<PostedKey, ReturnedClass>("DELETE", async (posted, qr, ct) =>
                {
                    // Retrieve the UID_AADGroup from the posted data
                    string uidgroup = posted.UID_AADGroup;

                    // Build a query to select all columns from the table where UID_AADGroup matches
                    var query1 = Query.From("AADGroup")
                                      .Select("*")
                                      .Where(string.Format("UID_AADGroup = '{0}'", uidgroup));

                    // Attempt to retrieve the entity from the database asynchronously
                    var tryget1 = await qr.Session.Source()
                                        .TryGetAsync(query1, EntityLoadType.DelayedLogic, ct)
                                        .ConfigureAwait(false);

                    // Check if the entity was successfully retrieved
                    if (tryget1.Success)
                    {
                        // Start a unit of work for transactional database operations
                        using (var u = qr.Session.StartUnitOfWork())
                        {
                            // Get the entity to be deleted
                            var objecttodelete = tryget1.Result;

                            // Mark the entity for deletion
                            objecttodelete.MarkForDeletion();

                            // Save the changes to the unit of work
                            await u.PutAsync(objecttodelete, ct).ConfigureAwait(false);

                            // Commit the unit of work to persist changes to the database
                            await u.CommitAsync(ct).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        // If the entity was not found, return an error with a custom message and error code
                        return await ReturnedClass.Error(
                            string.Format("No assignment was found with UID_AADGroup '{0}'.", uidgroup),
                            681
                        ).ConfigureAwait(false);
                    }

                    // Return a successful response by converting the entity to ReturnedClass
                    return await ReturnedClass.fromEntity(tryget1.Result, qr.Session).ConfigureAwait(false);
                }));

        }
    }
    // The ColumnData class represents a single column and its value in a database row
    public class ColumnData 
    {
        // The name of the column
        public string Column { get; set; }

        // The value of the column in the current row
        public string Value { get; set; }
    }
    public class PostedPreSQLParams
    {
        // The identifier of the predefined SQL statement to execute
        public string IdentQBMLimitedSQL { get; set; }
        // The additional parameters for the predefined SQL statement
        public string Department { get; set; }
        public string EmployeeType { get; set; }
        public string Description { get; set; }
        public string IdentityType { get; set; }
    }


    public class PostedKey
    {
        // Property to hold the UID_AADGroup as a string
        public string UID_AADGroup { get; set; }
    }
    public class PostedKeyEx4
    {
        // Properties to hold the IsMailEnabled and the UID_AADOrganization as a string
        public string UID_AADOrganization { get; set; }
        public string IsMailEnabled { get; set; }
    }
    public class PostedKeyEx42
    {
        // Properties to hold the UserPrincipalName and the UID_AADGroup as a string
        public string UserPrincipalName { get; set; }
        public string UID_AADGroup { get; set; }
    }
    public class ReturnedFields
    {
        // Properties to hold the fields of the group
        public string DisplayName { get; set; }
        public string UIDAADOrganization { get; set; }
        public string MailNickName { get; set; }
        public string UNSDisplay { get; set; }
        public string UIDAccProduct { get; set; }


        // Static method to create a ReturnedName instance from an IEntity object
        public static async Task<ReturnedFields> fromEntity(IEntity entity, ISession session)
        {
            // Instantiate a new ReturnedName object and populate it with data from the entity
            var g = new ReturnedFields
            {
                DisplayName = await entity.GetValueAsync<string>("DisplayName").ConfigureAwait(false),

                UIDAADOrganization = await entity.GetValueAsync<string>("UID_AADOrganization").ConfigureAwait(false),

                MailNickName = await entity.GetValueAsync<string>("MailNickName").ConfigureAwait(false),

                UNSDisplay = await entity.GetValueAsync<string>("UNSDisplay").ConfigureAwait(false),

                UIDAccProduct = await entity.GetValueAsync<string>("UID_AccProduct").ConfigureAwait(false),
            };
            return g;
        }
    }

    // Class to represent the posted data structure
    public class PostedID
    {
        public columnsarray[] columns { get; set; }  // Array of columns containing data
    }

    // Class to represent each column in the posted data
    public class columnsarray
    {
        public string column { get; set; }  // Name of the column
        public string value { get; set; }   // Value of the column
    }
    // Class representing the structure of the posted data
    public class PostedID_
    {
        // An array of columns representing the key(s) of the entity
        public columnsarray[] element { get; set; }

        // An array of columns representing the properties to update
        public columnsarray[] columns { get; set; }
    }

    // Class representing the data structure of the returned class (output to the client)
    public class ReturnedClass
    {
        // Property to hold the UID_AADGroup of the deleted entity
        public string uidgroup { get; set; }

        // Property to hold any error message
        public string errormessage { get; set; }

        // Static method to create a ReturnedClass instance from an IEntity object
        public static async Task<ReturnedClass> fromEntity(IEntity entity, ISession session)
        {
            // Instantiate a new ReturnedClass object
            var g = new ReturnedClass
            {
                // Asynchronously get the uidgroup value from the entity and assign it
                uidgroup = await entity.GetValueAsync<string>("UID_AADGroup").ConfigureAwait(false)
            };

            // Return the populated ReturnedClass object
            return g;
        }
        public static async Task<ReturnedClass> Error(string mess, int errorNumber)
        {
            // Throw an HTTP exception with the provided error number and message
            throw new System.Web.HttpException(errorNumber, mess);
        }

        // The ColumnData class represents a single column and its value in a database row
    }
}