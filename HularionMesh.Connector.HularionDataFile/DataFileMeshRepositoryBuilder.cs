#region License
/*
MIT License

Copyright (c) 2023 Johnathan A Drews

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
#endregion

using HularionCore.Pattern.Functional;
using HularionMesh.Domain;
using HularionMesh.Memory;
using HularionMesh.Repository;
using HularionMesh.Standard;
using HularionMesh.User;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace HularionMesh.Connector.HularionDataFile
{
    /// <summary>
    /// Creates a mesh repository using a file as a data souce.
    /// </summary>
    public class DataFileMeshRepositoryBuilder
    {
        /// <summary>
        /// Accumulates the details for registrations such as type include attributes and assemblies to search.
        /// </summary>
        public AssemblyRegistrationDetail RegistrationDetail { get; private set; } = new AssemblyRegistrationDetail();

        /// <summary>
        /// The directory in which to create the data file. Should not include the filename.
        /// </summary>
        public string Directory { get; set; }

        /// <summary>
        /// The name of the file to use or create.
        /// </summary>
        public string Filename { get; set; }

        /// <summary>
        /// The profile of the user creating the repository.
        /// </summary>
        public UserProfile UserProfile { get; set; } = UserProfile.DefaultUser;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="userProfile">The profile of the user creating the repository. Defaults to UserProfile.DefaultUser.</param>
        public DataFileMeshRepositoryBuilder(UserProfile userProfile = null)
        {
            UserProfile = userProfile;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="directory">The directory in which to create the data file. Should not include the filename.</param>
        /// <param name="filename">The name of the file to use or create.</param>
        /// <param name="userProfile">The profile of the user creating the repository. Defaults to UserProfile.DefaultUser.</param>
        public DataFileMeshRepositoryBuilder(string directory, string filename, UserProfile userProfile = null)
        {
            Directory = directory;
            Filename = filename;
            UserProfile = userProfile;
        }

        /// <summary>
        /// Builds the repository.
        /// </summary>
        /// <returns>The respository</returns>
        public FileRepository Build()
        {
            return CreateRepository(Directory, Filename, RegistrationDetail);
        }


        /// <summary>
        /// Creates a MeshRepository using a Postgres store with a standard setup.
        /// </summary>
        /// <typeparam name="IncludeAttributeType">The type of attribute that will cause a class to be added as a domain if that class has the attribute.</typeparam>
        /// <param name="directory">The directory in which the data file will be placed. DOes not include the filename.</param>
        /// <param name="fileName">The name of the file to create.</param>
        /// <param name="userProfile">The profile of the user creating the repository.</param>
        /// <returns>An in-memory MeshRepository</returns>
        public static FileRepository CreateRepository<IncludeAttributeType>(string directory, string fileName, UserProfile userProfile = null)
            where IncludeAttributeType : Attribute
        {
            var detail = AssemblyRegistrationDetail.CreateAssemblyRegistrationDetail(Assembly.GetCallingAssembly());
            detail.Assemblies.Add(typeof(IncludeAttributeType).Assembly);
            detail.SetRegistrationCheckerFromAttributes<IncludeAttributeType>();
            detail.InitializeDomainProperties = true;
            var result = CreateRepository(directory, fileName, detail, userProfile: userProfile);
            return result;
        }

        /// <summary>
        /// Creates a MeshRepository using a Postgres store with a standard setup.
        /// </summary>
        /// <typeparam name="IncludeAttributeType">The type of attribute that will cause a class to be added as a domain if that class has the attribute.</typeparam>
        /// <param name="directory">The directory in which the data file will be placed. DOes not include the filename.</param>
        /// <param name="fileName">The name of the file to create.</param>
        /// <param name="registrationDetail">The registration detail for the repository.</param>
        /// <param name="userProfile">The profile of the user creating the repository.</param>
        /// <returns>An in-memory MeshRepository</returns>
        public static FileRepository CreateRepository(string directory, string fileName, AssemblyRegistrationDetail registrationDetail, UserProfile userProfile = null)
        {
            var location = String.Format(@"{0}\{1}.hdf", directory.Trim(new char[] { '\\' }), fileName);
            var file = new FileAccessor(location);
            var provider = new HularionDataFileProvider(file);
            var repository = new MeshRepository(provider);
            repository.RegisterAssemblies(registrationDetail);
            provider.LoadFile();
            var result = new FileRepository(repository, provider);
            return result;

        }


    }
}
