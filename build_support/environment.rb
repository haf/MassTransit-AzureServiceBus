require 'semver' # gem install semver2
require_relative 'versioning'

task :common => :versioning do
    # .net/mono configuration management
    ENV['FRAMEWORK'] = FRAMEWORK = ENV['FRAMEWORK'] || (Rake::Win32::windows? ? "net40" : "mono28")
    puts "Framework: #{FRAMEWORK}"
end

task :release => :common do
  CONFIGURATION = 'Release'
end

task :debug => :common do
  CONFIGURATION = 'Debug'
end