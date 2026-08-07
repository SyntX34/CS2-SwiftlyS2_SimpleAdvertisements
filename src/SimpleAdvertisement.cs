using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared;

namespace SimpleAdvertisement;

[PluginMetadata(Id = "SimpleAdvertisement", Version = "1.0.0", Name = "SimpleAdvertisement", Author = "SyntX34", Description = "No description.")]
public partial class SimpleAdvertisement : BasePlugin {
  public SimpleAdvertisement(ISwiftlyCore core) : base(core)
  {
  }

  public override void ConfigureSharedInterface(IInterfaceManager interfaceManager) {
  }

  public override void UseSharedInterface(IInterfaceManager interfaceManager) {
  }

  public override void Load(bool hotReload) {
    
  }

  public override void Unload() {
  }
} 