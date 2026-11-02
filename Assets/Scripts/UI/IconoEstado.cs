using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RiftDeck.UI
{
    /// <summary>
    /// Ícono de un buff o debuff del enemigo, con los turnos que le quedan. Al pasar el dedo
    /// o el mouse encima (o al tocarlo) muestra qué hace y cuánto dura.
    /// </summary>
    public class IconoEstado : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        const float TiempoFijo = 3f;

        [SerializeField] UnityEngine.UI.Image icono;
        [SerializeField] TMP_Text turnos;

        public EstadoActivo Estado { get; private set; }
        public EnemyPanel Panel { get; set; }

        float fijoHasta;

        public void Mostrar(EstadoActivo estado)
        {
            Estado = estado;
            icono.sprite = estado.datos.icono;
            turnos.gameObject.SetActive(estado.turnos > 0);
            turnos.text = estado.turnos.ToString();
            turnos.color = estado.datos.esBuff ? PaletaUI.Menta : PaletaUI.Rosa;
        }

        /// <summary>Entra con un salto: 2 px arriba y vuelve.</summary>
        public System.Collections.IEnumerator Saltar()
        {
            var rt = icono.rectTransform;
            foreach (int dy in new[] { 4, 2, 1, 0 })
            {
                rt.anchoredPosition = new Vector2(0, dy);
                yield return new WaitForSeconds(0.05f);
            }
        }

        public void OnPointerEnter(PointerEventData e) => Panel?.MostrarTooltip(this);

        public void OnPointerExit(PointerEventData e)
        {
            if (Time.unscaledTime >= fijoHasta) Panel?.OcultarTooltip(this);
        }

        /// <summary>En el teléfono un toque lo deja abierto un rato.</summary>
        public void OnPointerClick(PointerEventData e)
        {
            fijoHasta = Time.unscaledTime + TiempoFijo;
            Panel?.MostrarTooltip(this);
            CancelInvoke(nameof(Cerrar));
            Invoke(nameof(Cerrar), TiempoFijo);
        }

        void Cerrar() => Panel?.OcultarTooltip(this);
    }
}
