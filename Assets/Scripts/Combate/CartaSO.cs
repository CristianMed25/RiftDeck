using UnityEngine;

namespace RiftDeck.Combate
{
    [CreateAssetMenu(menuName = "RiftDeck/Combate/Carta", fileName = "Carta")]
    public class CartaSO : ScriptableObject
    {
        public string id;
        public string nombreCarta;
        public int valorBase;
        public TipoDano tipoDano;
        public Sprite spriteIlustracion;
    }
}
