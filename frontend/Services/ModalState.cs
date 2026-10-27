namespace VetSystem.Frontend.Services;

// MainLayout marca el contenido de fondo como inert mientras haya un modal abierto: así el foco y el lector
// de pantalla no pueden salir del diálogo sin necesidad de JS
public class ModalState
{
    private int _open;

    public bool IsOpen => _open > 0;
    public event Action? Changed;

    public void Opened() { _open++; Changed?.Invoke(); }
    public void Closed() { _open = Math.Max(0, _open - 1); Changed?.Invoke(); }
}
