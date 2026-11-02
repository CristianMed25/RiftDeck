using System;
using UnityEngine;

namespace RiftDeck.UI
{
    /// <summary>
    /// Oro y cosméticos del teléfono, guardados en PlayerPrefs. Es del dispositivo, no de un
    /// jugador: J1 y J2 comparten el mismo teléfono. El oro se gana jugando (lo suma la
    /// pantalla de resultados del integrante 3 con <see cref="GanarOro"/>).
    /// </summary>
    public static class Inventario
    {
        const int OroInicial = 860;
        const string ClaveOro = "oro";

        public static event Action Cambio;

        public static int Oro => PlayerPrefs.GetInt(ClaveOro, OroInicial);

        public static void GanarOro(int cantidad)
        {
            PlayerPrefs.SetInt(ClaveOro, Oro + Mathf.Max(0, cantidad));
            Guardar();
        }

        public static bool Tiene(DatosCosmetico c) => c.DeSerie || PlayerPrefs.GetInt("tiene_" + c.id, 0) == 1;

        /// <summary>Id del equipado en esa categoría ("" = el de serie).</summary>
        public static string Equipado(CategoriaCosmetico categoria) => PlayerPrefs.GetString("equipado_" + categoria, "");

        public static bool EstaEquipado(DatosCosmetico c)
        {
            var id = Equipado(c.categoria);
            return id == c.id || (id == "" && c.DeSerie);
        }

        public static bool Comprar(DatosCosmetico c)
        {
            if (Tiene(c) || Oro < c.precio) return false;
            PlayerPrefs.SetInt(ClaveOro, Oro - c.precio);
            PlayerPrefs.SetInt("tiene_" + c.id, 1);
            Guardar();
            return true;
        }

        public static void Equipar(DatosCosmetico c)
        {
            if (!Tiene(c)) return;
            PlayerPrefs.SetString("equipado_" + c.categoria, c.id);
            Guardar();
        }

        /// <summary>Avisa a la UI después de borrar el progreso (ver <see cref="Ajustes.BorrarProgreso"/>).</summary>
        public static void Recargar() => Guardar();

        static void Guardar()
        {
            PlayerPrefs.Save();
            Cambio?.Invoke();
        }
    }
}
