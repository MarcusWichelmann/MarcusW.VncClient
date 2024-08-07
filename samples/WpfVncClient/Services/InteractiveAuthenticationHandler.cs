using System;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MarcusW.VncClient;
using MarcusW.VncClient.Protocol.SecurityTypes;
using MarcusW.VncClient.Security;

namespace WpfVncClient.Services;

[PublicAPI]
public class InteractiveAuthenticationHandler(SynchronizationContext? sync) : IAuthenticationHandler
{
    /// <inhertitdoc />
    public Task<TInput> ProvideAuthenticationInputAsync<TInput>(RfbConnection connection, ISecurityType securityType,
        IAuthenticationInputRequest<TInput> request) where TInput : class, IAuthenticationInput
    {
        if (typeof(TInput) != typeof(PasswordAuthenticationInput))
        {
            throw new InvalidOperationException(
                "The authentication input request is not supported by the interactive authentication handler.");
        }

        var password = "";
        void SetPassword(string p) => password = p;

        sync?.Send(_ => {
            var dlg = new EnterPasswordDialog();
            bool? result = dlg.ShowDialog();
            string p = result == true ? dlg.Password : "";
            SetPassword(p);
        }, null);

        return Task.FromResult((TInput)Convert.ChangeType(new PasswordAuthenticationInput(password), typeof(TInput)));
    }
}
