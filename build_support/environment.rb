require 'semver' # gem install semver2
require_relative 'versioning'

task :common => :versioning do
end

task :release => :common do
  CONFIGURATION = 'Release'
end

task :debug => :common do
  CONFIGURATION = 'Debug'
end