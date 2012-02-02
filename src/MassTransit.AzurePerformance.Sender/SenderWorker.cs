using System;
using System.Diagnostics;
using System.Net;
using Magnum;
using MassTransit.AzurePerformance.Messages;
using MassTransit.Transports.AzureServiceBus.Configuration;
using Microsoft.WindowsAzure.ServiceRuntime;
using log4net.Config;

namespace MassTransit.AzurePerformance.Sender
{
	public class SenderWorker : RoleEntryPoint
	{
		volatile bool _isStopping;

		public override void Run()
		{
			BasicConfigurator.Configure();
			Trace.WriteLine("Sender entry point called", "Information");
			RoleEnvironment.Stopping += (sender, args) => _isStopping = true;

			using (var sb = ServiceBusFactory.New(sbc =>
				{
					sbc.ReceiveFromComponents(
						AccountDetails.IssuerName,
						AccountDetails.Key,
						AccountDetails.Namespace,
						"sender"
						);

					sbc.UseAzureServiceBusRouting();
				}))
			{
				var receiver = sb.GetEndpoint(new Uri(string.Format("azure-sb://{0}:{1}@{2}/receiver", 
						AccountDetails.IssuerName,
						AccountDetails.Key,
						AccountDetails.Namespace)));

				while (!_isStopping)
				{
					var msg = new ZoomImpl { Id = CombGuid.Generate() };
					receiver.Send<ZoomZoom>(msg);
				}
			}
		}

		[Serializable]
		public class ZoomImpl : ZoomZoom, IEquatable<ZoomImpl>
		{
			public Guid Id { get; set; }

			public decimal Amount
			{
				get { return 1024m; }
			}

			public string Payload
			{
				get
				{
					return
@"𝕽aw denim whatever lo-fi DIY freegan bicycle rights, chambray gluten-free cupidatat squid. Chambray 
est ad farm-to-table scenester, PBR commodo fap nostrud odio. Letterpress sunt single-origin coffee, 
nesciunt tempor freegan consequat organic craft beer. Incididunt occaecat DIY gluten-free vinyl cardigan.
Austin irure enim brooklyn quinoa. Incididunt labore etsy thundercats, chambray marfa artisan VHS wolf
cardigan pitchfork. Proident butcher art party, high life farm-to-table stumptown nihil.

𝕰t tumblr skateboard next level occaecat dreamcatcher PBR, Austin locavore pariatur. Fugiat bicycle 
rights single-origin coffee, incididunt thundercats artisan next level 8-bit aesthetic irure cillum 
keffiyeh. Nulla thundercats fap organic, PBR 3 wolf moon biodiesel. Elit nulla mustache helvetica, 
seitan +1 ad mollit qui. Hoodie synth locavore cillum beard deserunt williamsburg sartorial. Dolor 
duis master cleanse, aliquip nisi tumblr banksy whatever aliqua sint retro esse 3 wolf moon laborum 
nesciunt. VHS trust fund irure fixie, retro high life biodiesel assumenda mlkshk seitan delectus american 
apparel freegan nostrud adipisicing.

Officia odio vinyl elit sapiente, +1 duis labore aesthetic fap trust fund. Nisi banksy master cleanse 
hoodie pariatur DIY, wes anderson artisan. Pariatur nisi skateboard, seitan accusamus master cleanse 
voluptate farm-to-table vice you probably haven't heard of them banh mi craft beer iphone. Minim ethical
wes anderson banh mi ad delectus. Shoreditch portland anim, quis artisan irure tumblr bicycle rights. Viral commodo ut, esse vegan mlkshk sustainable pariatur thundercats terry richardson jean shorts in. Fugiat williamsburg master cleanse, odio vinyl aute quis twee food truck single-origin coffee vegan vero.

℀ ℁ ℂ ℃ ℄ ℅ ℆ ℇ ℈ ℉ ℊ ℋ ℌ ℍ ℎ ℏ ℐ ℑ ℒ ℓ ℔ ℕ № ℗ ℘ ℙ ℚ ℛ ℜ ℝ ℞ ℟

𝕠 𝕡 𝕢 𝕣 𝕤 𝕥 𝕦 𝕧 𝕨 𝕩 𝕪 𝕫 𝕬 𝕭 𝕮 𝕯 𝕰 𝕱 𝕲 𝕳 𝕴 𝕵 𝕶 𝕷 𝕸 𝕹 𝕺 𝕻 𝕼 𝕽 𝕾 𝕿

Shoreditch food truck messenger bag banh mi placeat high life, nisi you probably haven't heard of them 
american apparel squid ea. Duis fugiat ullamco, vinyl viral consequat vegan eu deserunt minim placeat 
marfa cupidatat dreamcatcher. Etsy gluten-free mcsweeney's nulla raw denim organic. Brunch lo-fi bicycle 
rights viral locavore. Velit culpa dreamcatcher, id deserunt hoodie vegan nostrud enim ullamco mcsweeney's 
eu leggings in chambray. Laboris vinyl before they sold out, art party nesciunt viral seitan adipisicing. 
Fanny pack synth salvia 8-bit nisi cupidatat.";
				}
			}

			public bool Equals(ZoomImpl other)
			{
				if (ReferenceEquals(null, other)) return false;
				if (ReferenceEquals(this, other)) return true;
				return other.Id.Equals(Id);
			}

			public override bool Equals(object obj)
			{
				if (ReferenceEquals(null, obj)) return false;
				if (ReferenceEquals(this, obj)) return true;
				if (obj.GetType() != typeof (ZoomImpl)) return false;
				return Equals((ZoomImpl) obj);
			}

			public override int GetHashCode()
			{
				return Id.GetHashCode();
			}
		}

		public override bool OnStart()
		{
			// Set the maximum number of concurrent connections 
			ServicePointManager.DefaultConnectionLimit = 12;

			// For information on handling configuration changes
			// see the MSDN topic at http://go.microsoft.com/fwlink/?LinkId=166357.
			ConfigureDiagnostics();

			return base.OnStart();
		}

		void ConfigureDiagnostics()
		{
		}
	}
}
