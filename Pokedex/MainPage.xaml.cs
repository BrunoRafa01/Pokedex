using SQLite;
using System.Text.Json;

namespace Pokedex
{
    public class Pokemon
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string Nome { get; set; } = string.Empty;
        public string Tipo { get; set; } = string.Empty;
        public string Imagem { get; set; } = string.Empty;

        public string PoderesJson { get; set; } = "[]";

        [Ignore]
        public List<string> Poderes
        {
            get => string.IsNullOrEmpty(PoderesJson)
                   ? new List<string>()
                   : JsonSerializer.Deserialize<List<string>>(PoderesJson) ?? new List<string>();
            set => PoderesJson = JsonSerializer.Serialize(value);
        }

        [Ignore]
        public string PoderesFormatados => Poderes.Count > 0 ? string.Join(", ", Poderes) : "Nenhum";
    }

    public partial class MainPage : ContentPage
    {
        private SQLiteAsyncConnection? _banco;

        public MainPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InicializarBancoAsync();
            await AtualizarListaAsync();
        }

        private async Task InicializarBancoAsync()
        {
            if (_banco is not null)
                return;

            string caminhoBanco = Path.Combine(FileSystem.AppDataDirectory, "pokedex_dados.db3");
            _banco = new SQLiteAsyncConnection(caminhoBanco);
            await _banco.CreateTableAsync<Pokemon>();

            int total = await _banco.Table<Pokemon>().CountAsync();
            if (total == 0)
            {
                var iniciais = new List<Pokemon>
                {
                    new Pokemon
                    {
                        Nome = "Pikachu",
                        Tipo = "Elétrico",
                        Imagem = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/25.png",
                        Poderes = new List<string> { "Choque do Trovão", "Ataque Rápido", "Cauda de Ferro" }
                    },
                    new Pokemon
                    {
                        Nome = "Charizard",
                        Tipo = "Fogo / Voador",
                        Imagem = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/6.png",
                        Poderes = new List<string> { "Lança-Chamas", "Explosão de Fogo", "Golpe de Asa" }
                    },
                    new Pokemon
                    {
                        Nome = "Bulbasaur",
                        Tipo = "Planta / Venenoso",
                        Imagem = "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/1.png",
                        Poderes = new List<string> { "Chicote de Vinha", "Folha Navalha", "Raio Solar" }
                    }
                };

                await _banco.InsertAllAsync(iniciais);
            }
        }

        private async Task AtualizarListaAsync()
        {
            if (_banco is null) return;
            var pokemons = await _banco.Table<Pokemon>().ToListAsync();
            cvPokemons.ItemsSource = pokemons;
        }

        private async void OnSalvarClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtNome.Text) || string.IsNullOrWhiteSpace(txtTipo.Text))
            {
                await DisplayAlert("Aviso", "Informe pelo menos o Nome e o Tipo do Pokémon!", "OK");
                return;
            }

            var listaDePoderes = new List<string>();
            if (!string.IsNullOrWhiteSpace(txtPoderes.Text))
            {
                listaDePoderes = txtPoderes.Text
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .ToList();
            }

            var novo = new Pokemon
            {
                Nome = txtNome.Text.Trim(),
                Tipo = txtTipo.Text.Trim(),
                Imagem = string.IsNullOrWhiteSpace(txtImagem.Text)
                         ? "https://raw.githubusercontent.com/PokeAPI/sprites/master/sprites/pokemon/94.png"
                         : txtImagem.Text.Trim(),
                Poderes = listaDePoderes
            };

            await _banco!.InsertAsync(novo);

            txtNome.Text = string.Empty;
            txtTipo.Text = string.Empty;
            txtImagem.Text = string.Empty;
            txtPoderes.Text = string.Empty;

            await AtualizarListaAsync();
        }

        private async void OnExcluirClicked(object sender, EventArgs e)
        {
            if (sender is Button btn && btn.CommandParameter is Pokemon pokemonParaExcluir)
            {
                await _banco!.DeleteAsync(pokemonParaExcluir);
                await AtualizarListaAsync();
            }
        }
    }
}
