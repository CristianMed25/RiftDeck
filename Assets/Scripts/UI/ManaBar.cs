using TMPro;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Maná estilo Legends of Runeterra: 10 gemas (las que pasan del máximo del turno
    /// se ven bloqueadas) y 3 de reserva con el maná que no se gastó. Al elegir cartas,
    /// las gemas que se van a gastar se pintan de Oro; primero se gasta el maná normal
    /// y después la reserva.
    /// </summary>
    public class ManaBar : MonoBehaviour
    {
        public const int MaxMana = 10;
        public const int MaxReserva = 3;

        static readonly Color Bloqueada = new Color(0.106f, 0.063f, 0.2f, 0.6f);

        [SerializeField] UnityEngine.UI.Image[] gemas = new UnityEngine.UI.Image[MaxMana];
        [SerializeField] UnityEngine.UI.Image[] reserva = new UnityEngine.UI.Image[MaxReserva];
        [SerializeField] TMP_Text numero;
        [SerializeField] TMP_Text maximo;

        public void Mostrar(int mana, int maxTurno, int enReserva, int costoElegido)
        {
            int deMana = Mathf.Min(costoElegido, mana);
            int deReserva = costoElegido - deMana;

            numero.text = (mana - deMana).ToString();
            maximo.text = "/" + maxTurno;

            for (int i = 0; i < gemas.Length; i++)
                gemas[i].color = i >= maxTurno ? Bloqueada
                    : i >= mana ? PaletaUI.Hueco
                    : i >= mana - deMana ? PaletaUI.Oro
                    : PaletaUI.Azul;

            for (int i = 0; i < reserva.Length; i++)
                reserva[i].color = i >= enReserva ? PaletaUI.Hueco
                    : i >= enReserva - deReserva ? PaletaUI.Oro
                    : PaletaUI.VioletaReserva;
        }
    }
}
