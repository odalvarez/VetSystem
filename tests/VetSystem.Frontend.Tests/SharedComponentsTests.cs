using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using VetSystem.Frontend.Components;
using VetSystem.Frontend.Services;

namespace VetSystem.Frontend.Tests;

public class SharedComponentsTests : TestContext
{
    [Theory]
    [InlineData("Scheduled", "badge-scheduled", "Programada")]
    [InlineData("confirmed", "badge-confirmed", "Confirmada")]
    [InlineData("NoShow", "badge-no_show", "No asistió")]
    [InlineData("no show", "badge-no_show", "No asistió")]
    [InlineData("failed", "badge-failed", "Fallida")]
    [InlineData("whatsapp", "badge-whatsapp", "WhatsApp")]
    [InlineData("overdue", "badge-overdue", "Vencida")]
    [InlineData("upcoming", "badge-upcoming", "Próxima")]
    [InlineData("ok", "badge-ok", "Al día")]
    [InlineData("active", "badge-active", "Activo")]
    [InlineData("inactive", "badge-inactive", "Inactivo")]
    [InlineData("open", "badge-open", "Abierta")]
    [InlineData("closed", "badge-closed", "Cerrada")]
    public void StatusBadge_MapsEveryStatusToItsClassAndLabel(string status, string cssClass, string label)
    {
        var cut = RenderComponent<StatusBadge>(p => p.Add(x => x.Status, status));

        Assert.Contains(cssClass, cut.Find("span.badge").ClassList);
        Assert.Contains(label, cut.Markup);
    }

    [Fact]
    public void StatusBadge_UnknownStatus_IsShownAsIsWithNeutralStyle()
    {
        var cut = RenderComponent<StatusBadge>(p => p.Add(x => x.Status, "Archived"));

        Assert.Contains("badge-unknown", cut.Find("span.badge").ClassList);
        Assert.Contains("Archived", cut.Markup);
    }

    [Fact]
    public void Icon_PointsToTheSpriteAndIsHiddenFromScreenReaders()
    {
        var cut = RenderComponent<Icon>(p => p.Add(x => x.Name, "calendar"));

        Assert.Equal("true", cut.Find("svg").GetAttribute("aria-hidden"));
        Assert.Equal("icons.svg#calendar", cut.Find("use").GetAttribute("href"));
    }

    [Fact]
    public void ModalShell_ExposesDialogSemantics_AndTracksOpenState()
    {
        var modals = new ModalState();
        Services.AddSingleton(modals);
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = RenderComponent<ModalHost>(host => host.AddChildContent<ModalShell>(p => p
            .Add(x => x.Title, "Solicitar cita")
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "contenido")))));

        var dialog = cut.Find("[role=dialog]");
        Assert.Equal("true", dialog.GetAttribute("aria-modal"));
        Assert.Equal("Solicitar cita", cut.Find("#" + dialog.GetAttribute("aria-labelledby")).TextContent);
        Assert.True(modals.IsOpen);
    }

    [Fact]
    public void ModalShell_EscapeInvokesOnClose_AndBackdropOnlyWhenEnabled()
    {
        Services.AddSingleton(new ModalState());
        JSInterop.Mode = JSRuntimeMode.Loose;
        var closed = 0;
        var close = EventCallback.Factory.Create(this, () => closed++);

        var cut = RenderComponent<ModalHost>(host => host.AddChildContent<ModalShell>(p => p
            .Add(x => x.Title, "Detalle")
            .Add(x => x.OnClose, close)));

        cut.Find(".modal-overlay").KeyDown(new KeyboardEventArgs { Key = "Escape" });
        Assert.Equal(1, closed);

        cut.Find(".modal-overlay").Click();
        Assert.Equal(1, closed); // los formularios no se cierran al hacer clic fuera: se perdería lo escrito
    }

    [Fact]
    public void ModalShell_ReleasesTheBackgroundWhenDisposed()
    {
        var modals = new ModalState();
        Services.AddSingleton(modals);
        JSInterop.Mode = JSRuntimeMode.Loose;

        var cut = RenderComponent<ModalHost>(host => host.AddChildContent<ModalShell>(p => p.Add(x => x.Title, "x")));
        Assert.True(modals.IsOpen);

        DisposeComponents();

        Assert.False(modals.IsOpen);
    }
}
