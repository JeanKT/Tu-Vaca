namespace TuVaca.Models
{
    public class UsuarioModel
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public string Documento { get; set; } // RN-1-CU01: Único en el sistema[cite: 1]
        public string Correo { get; set; }    // RN-2-CU01: Único en el sistema[cite: 1]
        public string Contrasena { get; set; }// RN-3-CU01: Mínimo 8 caracteres, letra, número y especial[cite: 1]
        public string Rol { get; set; }       // RN-4-CU01: Productor/Vendedor o Comprador/Visitante[cite: 1]
    }
}