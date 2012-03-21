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

task :ensure_packages do
  Dir.glob("./src/**/packages.config") do |cfg|
    sh %Q[src/.nuget/NuGet.exe install "#{cfg}" -o "src/packages"] do |ok, res|
      puts (res.inspect) unless ok
    end
  end
end

task :compile => [:ensure_packages, :ensure_account_details] do
  sh 'msbuild src/MassTransit-AzureServiceBus.sln'
end

nunit :test => [:ensure_account_details, :release] do |n|
  asms = Dir.glob("./src/MassTransit.*.Tests/bin/#{CONFIGURATION}/*.Tests.dll")
  puts asms.inspect
  n.command = Dir.glob('./src/packages/NUnit*/Tools/nunit-console.exe').first
  n.assemblies = asms
  n.options '/framework=net-4.0'
end

task :default => [:release, :compile, :test]