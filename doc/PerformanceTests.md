## Running Perf-tests

If you wish to run the performance tests, you will have to change `src\MassTransit.AzurePerformance\ServiceConfiguration.Cloud.cscfg` to reflect your own values. This is what it looks for me:

```
<?xml version="1.0" encoding="utf-8"?>
<ServiceConfiguration serviceName="MassTransit.AzurePerformance.Sender" xmlns="http://schemas.microsoft.com/ServiceHosting/2008/10/ServiceConfiguration" osFamily="1" osVersion="*">
	<Role name="MassTransit.AzurePerformance.Receiver">
		<Instances count="1" />
		<ConfigurationSettings>
			<Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="DefaultEndpointsProtocol=https;AccountName=[YOUR ACCOUNT NAME];AccountKey=[YOUR ACCOUNT KEY]" />
			<Setting name="RampUpCount" value="50"/>
			<Setting name="SampleSize" value="5000"/>
			<Setting name="WaitForNumberOfSenders" value="4"/>
		</ConfigurationSettings>
	</Role>
	<Role name="MassTransit.AzurePerformance.Sender">
		<Instances count="4" />
		<ConfigurationSettings>
			<Setting name="Microsoft.WindowsAzure.Plugins.Diagnostics.ConnectionString" value="DefaultEndpointsProtocol=https;AccountName=[YOUR ACCOUNT NAME];AccountKey=[YOUR ACCOUNT KEY]" />
		</ConfigurationSettings>
	</Role>
</ServiceConfiguration>
```