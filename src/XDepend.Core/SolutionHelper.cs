using Microsoft.Build.Construction;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace XDepend.Core
{
    public static class SolutionHelper
    {
        public static IEnumerable<string> GetProjectFilePathsInSolution(string solutionFilePath)
        {
            if (!File.Exists(solutionFilePath))
            {
                throw new FileNotFoundException("Solution file not found", solutionFilePath);
            }

            try
            {
                var solutionFile = SolutionFile.Parse(solutionFilePath);

                var projectFilePaths = solutionFile.ProjectsInOrder
                    .Where(p => p.ProjectType == SolutionProjectType.KnownToBeMSBuildFormat)
                    .Select(p => p.AbsolutePath)
                    .Where(p => p.EndsWith(".csproj") || p.EndsWith(".vbproj"));

                return projectFilePaths;
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("Error reading solution file", ex);
            }
        }


        public static IEnumerable<string> GetProjectFilePathsInDirectory(string directoryPath)
        {
            return Directory.GetFiles(directoryPath, "*.csproj", SearchOption.AllDirectories)
                .Concat(Directory.GetFiles(directoryPath, "*.vbproj", SearchOption.AllDirectories));
        }

        public static IEnumerable<string> GetProjectDependencies(string projectFilePath)
        {
            var projectFileContent = File.ReadAllText(projectFilePath);
            var projectFileElement = XElement.Parse(projectFileContent);
            return projectFileElement
                .Descendants("ProjectReference")
                .Select(e => e.Attribute("Include").Value)
                .Select(p => Path.Combine(Path.GetDirectoryName(projectFilePath), p));
        }

        public static IEnumerable<string> GetPackageReferences(string projectFilePath)
        {
            var projectFileContent = File.ReadAllText(projectFilePath);
            var projectFileElement = XElement.Parse(projectFileContent);
            return projectFileElement
                .Descendants("PackageReference")
                .Select(e => e.Attribute("Include").Value);
        }

        public static IEnumerable<string> GetProjectReferences(string projectFilePath)
        {
            var projectFileContent = File.ReadAllText(projectFilePath);
            var projectFileElement = XElement.Parse(projectFileContent);
            return projectFileElement
                .Descendants("Reference")
                .Where(e => e.Attribute("Include") != null)
                .Select(e => e.Attribute("Include").Value);
        }

        public static IEnumerable<string> GetSolutionReferences(string solutionFilePath)
        {
            var projectFilePaths = GetProjectFilePathsInSolution(solutionFilePath);

            var projectReferences = projectFilePaths.SelectMany(p => GetProjectReferences(p));
            var packageReferences = projectFilePaths.SelectMany(p => GetPackageReferences(p));

            return projectReferences.Concat(packageReferences);
        }

        public static IEnumerable<string> GetSolutionPackages(string solutionFilePath)
        {
            var projectFilePaths = GetProjectFilePathsInSolution(solutionFilePath);
            return projectFilePaths.SelectMany(p => GetPackageReferences(p)).Distinct();
        }

        public static IEnumerable<string> GetProjectPackages(string solutionFilePath)
        {
            return GetPackageReferences(solutionFilePath);
        }
    }
}