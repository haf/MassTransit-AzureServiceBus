# Copyright Henrik Feldt 2012
require 'albacore' # gem install albacore
require 'fileutils' #in ruby core
require 'semver' #gem install semver2
require_relative 'build_support/environment'

task :ensure_account_details do
  targ = 'src/MassTransit.Transports.AzureServiceBus.Tests/Framework/AccountDetails.cs'
  unless File.exists? targ then ; FileUtils.cp 'build_support/AccountDetails.cs', targ ; end
  targ = 'src/MassTransit.Async/AccountDetails.fs'
  unless File.exists? targ then ; FileUtils.cp 'build_support/AccountDetails.fs', targ ; end
  targ = 'src/PerformanceTesting/MassTransit.AzurePerformance/ServiceConfiguration.Cloud.cscfg'
  unless File.exists? targ then ; FileUtils.cp 'build_support/ServiceConfiguration.Cloud.cscfg', targ ; end
end

desc "Ensure that all NuGet packages are here"
task :ensure_packages do
  Dir.glob("./src/**/packages.config") do |cfg|
    sh %Q[src/.nuget/NuGet.exe install "#{cfg}" -o "src/packages"] do |ok, res|
      puts (res.inspect) unless ok
    end
  end
end

desc "Compile Solution"
msbuild :compile => [:ensure_packages, :ensure_account_details] do |msb|
  msb.solution = 'src/MassTransit-AzureServiceBus.sln'
  msb.properties :Configuration => CONFIGURATION
  msb.targets    :Build
  msb.verbosity = "minimal"
end

desc "Run Tests"
nunit :test => [:ensure_account_details, :release, :compile] do |n|
  asms = Dir.glob("#{File.dirname(__FILE__)}/src/MassTransit.*.Tests/bin/#{CONFIGURATION}/*.Tests.dll")
  puts "Running nunit with assemblies: #{asms.inspect}"
  n.command = Dir.glob("#{File.dirname(__FILE__)}/src/packages/NUnit*/Tools/nunit-console.exe").first
  n.assemblies = asms
  n.options '/framework=net-4.0'
end

desc "Compile Solution, Run Tests"
task :default => [:release, :compile, :test]