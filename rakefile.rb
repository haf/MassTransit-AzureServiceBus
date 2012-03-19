# Copyright Henrik Feldt 2012

require 'albacore'
require 'fileutils'

task :ensure_packages do
  Dir.glob("./src/**/packages.config") do |cfg|
    sh %Q[src/.nuget/NuGet.exe install "#{cfg}" -o "src/packages"] do |ok, res|
      puts (res.inspect) unless ok
    end
  end
end

task :compile => :ensure_packages do
  sh 'msbuild src/MassTransit-AzureServiceBus.sln'
end

task :test do
  puts "TODO: test"
end

task :default => [:compile, :test]