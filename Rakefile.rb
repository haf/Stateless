require "rubygems"
require "bundler"
Bundler.setup
$: << './'

require 'albacore'
require 'rake/clean'
require 'semver'

require 'buildscripts/utils'
require 'buildscripts/paths'
require 'buildscripts/project_details'
require 'buildscripts/environment'

# to get the current version of the project, type 'SemVer.find.to_s' in this rake file.

desc 'generate the shared assembly info'
assemblyinfo :assemblyinfo => ["env:release"] do |asm|
  data = commit_data() #hash + date
  asm.product_name = asm.title = PROJECTS[:s][:title]
  asm.description = PROJECTS[:s][:description] + " #{data[0]} - #{data[1]}"
  asm.company_name = PROJECTS[:s][:company]
  # This is the version number used by framework during build and at runtime to locate, link and load the assemblies. When you add reference to any assembly in your project, it is this version number which gets embedded.
  asm.version = BUILD_VERSION
  # Assembly File Version : This is the version number given to file as in file system. It is displayed by Windows Explorer. Its never used by .NET framework or runtime for referencing.
  asm.file_version = BUILD_VERSION
  asm.custom_attributes :AssemblyInformationalVersion => "#{BUILD_VERSION}", # disposed as product version in explorer
    :CLSCompliantAttribute => false,
    :AssemblyConfiguration => "#{CONFIGURATION}",
    :Guid => PROJECTS[:s][:guid]
  asm.com_visible = false
  asm.copyright = PROJECTS[:s][:copyright]
  asm.output_file = File.join(FOLDERS[:src], 'SharedAssemblyInfo.cs')
  asm.namespaces = "System", "System.Reflection", "System.Runtime.InteropServices", "System.Security"
end


desc "build sln file"
msbuild :msbuild do |msb|
  msb.solution   = FILES[:sln]
  msb.properties :Configuration => CONFIGURATION
  msb.targets    :Clean, :Build
end


task :s_output => [:msbuild] do
  target = File.join(FOLDERS[:binaries], PROJECTS[:s][:id])
  copy_files FOLDERS[:s][:out], "*.{xml,dll,pdb,config}", target
  CLEAN.include(target)
end

task :output => [:s_output]
task :nuspecs => [:s_nuspec]

desc "Create a nuspec for 'Stateless'"
nuspec :s_nuspec do |nuspec|
  nuspec.id = "#{PROJECTS[:s][:nuget_key]}"
  nuspec.version = BUILD_VERSION
  nuspec.authors = "#{PROJECTS[:s][:authors]}"
  nuspec.description = "#{PROJECTS[:s][:description]}"
  nuspec.summary = "#{PROJECTS[:s][:summary]}"
  nuspec.title = "#{PROJECTS[:s][:title]}"
  nuspec.iconUrl = 'http://code.google.com/p/stateless/logo?cct=1252864008'
  nuspec.projectUrl = 'http://github.com/haf/Stateless'
  nuspec.language = "en-US"
  nuspec.licenseUrl = "http://www.apache.org/licenses/LICENSE-2.0" # TODO: set this for nuget generation
  nuspec.requireLicenseAcceptance = "false"
  nuspec.tags = "statemachine, stateless"
  nuspec.output_file = FILES[:s][:nuspec]
  nuspec_copy(:s, "#{PROJECTS[:s][:id]}.{dll,pdb,xml}")
end

task :nugets => [:"env:release", :nuspecs, :s_nuget]

desc "nuget pack 'Stateless'"
nugetpack :s_nuget do |nuget|
   nuget.command     = "#{COMMANDS[:nuget]}"
   nuget.nuspec      = "#{FILES[:s][:nuspec]}"
   # nuget.base_folder = "."
   nuget.output      = "#{FOLDERS[:nuget]}"
end

task :publish => [:"env:release", :s_nuget_push]

desc "publishes (pushes) the nuget package 'Stateless'"
nugetpush :s_nuget_push do |nuget|
  nuget.command = "#{COMMANDS[:nuget]}"
  nuget.package = "#{File.join(FOLDERS[:nuget], PROJECTS[:s][:nuget_key] + "." + BUILD_VERSION + '.nupkg')}"
# nuget.apikey = "...."
  nuget.source = URIS[:nuget_offical]
  nuget.create_only = false
end

task :default  => ["env:release", "assemblyinfo", "msbuild", "output", "nugets"]