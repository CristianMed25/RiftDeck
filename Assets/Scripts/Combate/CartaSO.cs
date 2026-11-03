using UnityEngine;

namespace RiftDeck.Combate
{
    [CreateAssetMenu(menuName = "RiftDeck/Combate/Carta", fileName = "Carta")]
    public class CartaSO : ScriptableObject
    {
        public string id;
        public string nombreCarta;
        [Min(0)] public int costo = 1;
        public int valorBase;
        public TipoDano tipoDano;
        public Sprite spriteIlustracion;

        public bool TieneIlustracion => spriteIlustracion != null;
    }
}
