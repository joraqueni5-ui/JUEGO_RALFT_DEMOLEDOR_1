using UnityEngine;
using UnityEngine.EventSystems;

namespace DemoledorUnifranz.UI.Menus
{
    /// <summary>
    /// Franja de un piso sobre el edificio: al pasar el mouse o elegirla con el teclado,
    /// pide al menú que muestre la ficha de ese piso.
    /// </summary>
    public class BandaPisoUI : MonoBehaviour, IPointerEnterHandler, ISelectHandler
    {
        public MenuNivelesArcade menu;
        public int indice;

        public void OnPointerEnter(PointerEventData datos)
        {
            if (menu == null) return;
            menu.Seleccionar(indice);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameObject);
        }

        public void OnSelect(BaseEventData datos)
        {
            if (menu != null) menu.Seleccionar(indice);
        }
    }
}
