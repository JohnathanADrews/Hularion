#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionMesh.Repository;
using HularionMesh.User;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace HularionMesh.Connector.Sqlite
{
    /// <summary>
    /// A repository builder for a Sqlite connector.
    /// </summary>
    public class SqliteMeshRepositoryBuilder
    {
        /// <summary>
        /// Creates a MeshRepository using a Sqlite store with a standard setup.
        /// </summary>
        /// <typeparam name="IncludeAttributeType">The type of attribute that will cause a class to be added as a domain if that class has the attribute.</typeparam>
        /// <param name="directory">The directory in which to create the SQLite store.</param>
        /// <param name="databaseName">The name of the database file.</param>
        /// <param name="userProfile">The profile of the user creating the repository.</param>
        /// <param name="useExisting">If false, any existing database with that name will be overwritten. If true (default), any current database with the matching name will be used.</param>
        /// <returns>A mesh repository with a SQLite store.</returns>
        public static MeshRepository CreateRepository<IncludeAttributeType>(string directory, string databaseName = null, UserProfile userProfile = null, bool useExisting = true)
            where IncludeAttributeType : Attribute
        {
            if (databaseName == null) { databaseName = MeshKey.CreateUniqueTag(); }
            var location = String.Format(@"{0}\{1}.db", directory.Trim(new char[] { '\\' }), databaseName);
            if (!Directory.Exists(directory)) { Directory.CreateDirectory(directory); }
            if (!useExisting && File.Exists(location)) { File.Delete(location); }
            var provider = new SqliteMeshService(String.Format("DataSource={0}", location));
            var detail = AssemblyRegistrationDetail.CreateAssemblyRegistrationDetail(Assembly.GetCallingAssembly());
            detail.InitializeDomainProperties = true;
            detail.SetRegistrationCheckerFromAttributes<IncludeAttributeType>();
            detail.UniqueNameProvider = TypeToDomainName.DefaultProvider;
            var repository = new MeshRepository(provider);
            repository.RegisterAssemblies(detail);
            return repository;
        }
    }
}
