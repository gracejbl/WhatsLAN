namespace WhatsLAN.Core.Services;

public record EmojiItem(string Emoji, string Name, string Category);

public class EmojiService
{
    private static EmojiService? _instance;
    public static EmojiService Instance => _instance ??= new EmojiService();

    private readonly List<EmojiItem> _allEmojis = [];
    private readonly List<string> _recentEmojis = [];
    private const int MaxRecent = 30;

    public IReadOnlyList<string> Categories { get; } =
    [
        "Récents",
        "Smileys",
        "Personnes",
        "Animaux",
        "Nourriture",
        "Activités",
        "Voyages",
        "Objets",
        "Symboles"
    ];

    public EmojiService()
    {
        InitializeDatabase();
    }

    public void AddRecent(string emoji)
    {
        _recentEmojis.Remove(emoji);
        _recentEmojis.Insert(0, emoji);
        if (_recentEmojis.Count > MaxRecent)
        {
            _recentEmojis.RemoveAt(_recentEmojis.Count - 1);
        }
    }

    public List<EmojiItem> GetEmojisByCategory(string category)
    {
        if (category == "Récents")
        {
            var list = new List<EmojiItem>();
            foreach (var r in _recentEmojis)
            {
                var match = _allEmojis.FirstOrDefault(e => e.Emoji == r);
                list.Add(match ?? new EmojiItem(r, "Récent", "Récents"));
            }
            if (list.Count == 0)
            {
                // Par défaut quelques émojis populaires si récents est vide
                return
                [
                    new("👍", "pouce j'aime", "Récents"),
                    new("❤️", "coeur rouge amour", "Récents"),
                    new("😂", "rire larmes joie", "Récents"),
                    new("😊", "sourire content", "Récents"),
                    new("🙏", "prière merci", "Récents"),
                    new("🔥", "feu flamme", "Récents")
                ];
            }
            return list;
        }

        return _allEmojis.Where(e => e.Category == category).ToList();
    }

    public List<EmojiItem> Search(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return _allEmojis.Take(40).ToList();
        }

        var q = query.Trim().ToLowerInvariant();
        return _allEmojis
            .Where(e => e.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private void InitializeDatabase()
    {
        // Smileys
        Add("😀", "sourire joyeux", "Smileys");
        Add("😃", "grand sourire", "Smileys");
        Add("😄", "sourire yeux rieurs", "Smileys");
        Add("😁", "sourire éclatant", "Smileys");
        Add("😆", "rire plissé", "Smileys");
        Add("😅", "sourire sueur", "Smileys");
        Add("😂", "mort de rire larmes joie", "Smileys");
        Add("🤣", "rouler par terre de rire", "Smileys");
        Add("😊", "sourire rougissant", "Smileys");
        Add("😇", "ange innocent", "Smileys");
        Add("🙂", "petit sourire", "Smileys");
        Add("🙃", "tête en bas", "Smileys");
        Add("😉", "clin d'oeil", "Smileys");
        Add("😌", "soulagé serein", "Smileys");
        Add("😍", "yeux coeur amour", "Smileys");
        Add("🥰", "coeurs amour affection", "Smileys");
        Add("😘", "bisou baiser coeur", "Smileys");
        Add("😋", "miam délicieux langue", "Smileys");
        Add("😛", "langue", "Smileys");
        Add("😜", "clin d'oeil langue", "Smileys");
        Add("🤪", "fou zany", "Smileys");
        Add("😝", "yeux fermés langue", "Smileys");
        Add("🤑", "argent dollars", "Smileys");
        Add("🤗", "câlin bras ouverts", "Smileys");
        Add("🤭", "main sur la bouche", "Smileys");
        Add("🤫", "chut silence secret", "Smileys");
        Add("🤔", "penseur réflexion doute", "Smileys");
        Add("🤐", "bouche cousue", "Smileys");
        Add("🤨", "sourcil levé sceptique", "Smileys");
        Add("😐", "neutre sans expression", "Smileys");
        Add("😑", "sans commentaire", "Smileys");
        Add("😶", "sans bouche", "Smileys");
        Add("😏", "sourire en coin malin", "Smileys");
        Add("😒", "blasé agacé", "Smileys");
        Add("🙄", "yeux au ciel", "Smileys");
        Add("😬", "grimace gêné", "Smileys");
        Add("🤥", "menteur pinocchio", "Smileys");
        Add("😌", "apaisé", "Smileys");
        Add("😔", "triste pensif", "Smileys");
        Add("😪", "somnolent", "Smileys");
        Add("🤤", "salive envie", "Smileys");
        Add("😴", "dort sommeil zzz", "Smileys");
        Add("😷", "masque malade", "Smileys");
        Add("🤒", "thermomètre fièvre", "Smileys");
        Add("🤕", "bandage blessé", "Smileys");
        Add("🤢", "nausée dégoût", "Smileys");
        Add("🤮", "vomir", "Smileys");
        Add("🤧", "éternuement rhume", "Smileys");
        Add("🥵", "chaud canicule", "Smileys");
        Add("🥶", "froid gelé", "Smileys");
        Add("🥴", "étourdi éméché", "Smileys");
        Add("😵", "étourdi k.o.", "Smileys");
        Add("🤯", "tête qui explose", "Smileys");
        Add("😎", "lunettes de soleil cool", "Smileys");
        Add("🤓", "intello nerd lunettes", "Smileys");
        Add("🧐", "monocle inspection", "Smileys");
        Add("😕", "confus perplexe", "Smileys");
        Add("😟", "inquiet souci", "Smileys");
        Add("🙁", "légèrement triste", "Smileys");
        Add("😮", "bouche bée waouh surprise", "Smileys");
        Add("😯", "surpris ébahi", "Smileys");
        Add("😲", "stupéfait étonné", "Smileys");
        Add("😳", "gêné rouge", "Smileys");
        Add("🥺", "yeux doux s'il te plaît", "Smileys");
        Add("😦", "déçu surpris", "Smileys");
        Add("😧", "angoissé", "Smileys");
        Add("😨", "peur frayeur", "Smileys");
        Add("😰", "anxieux sueur froide", "Smileys");
        Add("😥", "déçu mais soulagé", "Smileys");
        Add("😢", "larme triste pleure", "Smileys");
        Add("😭", "pleurs torrent larmes", "Smileys");
        Add("😱", "cri effroi peur panique", "Smileys");
        Add("😖", "frustré agacé", "Smileys");
        Add("😣", "persévérant souffrance", "Smileys");
        Add("😞", "déception abattu", "Smileys");
        Add("😓", "fatigué découragé", "Smileys");
        Add("😩", "épuisé lassé", "Smileys");
        Add("😫", "très fatigué", "Smileys");
        Add("🥱", "bâillement ennui", "Smileys");
        Add("😤", "fumée narines triomphant", "Smileys");
        Add("😡", "enragé colère rouge", "Smileys");
        Add("😠", "fâché mécontent", "Smileys");

        // Personnes & Gestes
        Add("👋", "salut coucou au revoir main", "Personnes");
        Add("🤚", "dos de la main", "Personnes");
        Add("🖐️", "main ouverte", "Personnes");
        Add("✋", "main levée stop", "Personnes");
        Add("🖖", "salut vulcain spock", "Personnes");
        Add("👌", "d'accord parfait ok", "Personnes");
        Add("🤌", "geste italien qu'est-ce que", "Personnes");
        Add("🤏", "un tout petit peu", "Personnes");
        Add("✌️", "victoire paix peace deux", "Personnes");
        Add("🤞", "doigts croisés chance", "Personnes");
        Add("🤟", "je t'aime rock", "Personnes");
        Add("🤘", "signe des cornes rock métal", "Personnes");
        Add("🤙", "appelle-moi shaka", "Personnes");
        Add("👈", "doigt pointé gauche", "Personnes");
        Add("👉", "doigt pointé droite", "Personnes");
        Add("👆", "doigt vers le haut", "Personnes");
        Add("👇", "doigt vers le bas", "Personnes");
        Add("☝️", "index levé une minute", "Personnes");
        Add("👍", "pouce levé j'aime bravo super d'accord", "Personnes");
        Add("👎", "pouce baissé nul pas d'accord", "Personnes");
        Add("✊", "poing levé force", "Personnes");
        Add("👊", "poing en avant check", "Personnes");
        Add("🤛", "check gauche", "Personnes");
        Add("🤜", "check droite", "Personnes");
        Add("👏", "applaudissements bravo mains", "Personnes");
        Add("🙌", "mains en l'air célébration", "Personnes");
        Add("👐", "mains ouvertes accueil", "Personnes");
        Add("🤲", "mains paumes levées prière", "Personnes");
        Add("🤝", "poignée de main accord contrat", "Personnes");
        Add("🙏", "prière s'il vous plaît merci namasté", "Personnes");

        // Animaux & Nature
        Add("🐶", "chien chiot toutou", "Animaux");
        Add("🐱", "chat chaton matou", "Animaux");
        Add("🐭", "souris", "Animaux");
        Add("🐹", "hamster", "Animaux");
        Add("🐰", "lapin", "Animaux");
        Add("🦊", "renard", "Animaux");
        Add("🐻", "ours", "Animaux");
        Add("🐼", "panda", "Animaux");
        Add("🐨", "koala", "Animaux");
        Add("🐯", "tigre", "Animaux");
        Add("🦁", "lion", "Animaux");
        Add("🐮", "vache", "Animaux");
        Add("🐷", "cochon", "Animaux");
        Add("🐸", "grenouille", "Animaux");
        Add("🐵", "singe", "Animaux");
        Add("🐔", "poule poulet", "Animaux");
        Add("🐧", "pingouin", "Animaux");
        Add("🐦", "oiseau", "Animaux");
        Add("🦆", "canard", "Animaux");
        Add("🦅", "aigle", "Animaux");
        Add("🦉", "chouette hibou", "Animaux");
        Add("🦇", "chauve-souris", "Animaux");
        Add("🐺", "loup", "Animaux");
        Add("🦄", "licorne", "Animaux");
        Add("🐝", "abeille", "Animaux");
        Add("🐛", "chenille insecte", "Animaux");
        Add("🦋", "papillon", "Animaux");
        Add("🐌", "escargot", "Animaux");
        Add("🌸", "fleur cerisier printemps", "Animaux");
        Add("🌹", "rose amour fleur", "Animaux");
        Add("🌻", "tournesol soleil", "Animaux");
        Add("🌴", "palmier été vacances plage", "Animaux");
        Add("🌲", "arbre sapin forêt", "Animaux");
        Add("🍀", "trèfle quatre feuilles chance", "Animaux");

        // Nourriture
        Add("🍏", "pomme verte", "Nourriture");
        Add("🍎", "pomme rouge", "Nourriture");
        Add("🍐", "poire", "Nourriture");
        Add("🍊", "orange clémentine", "Nourriture");
        Add("🍋", "citron", "Nourriture");
        Add("🍌", "banane", "Nourriture");
        Add("🍉", "pastèque", "Nourriture");
        Add("🍇", "raisin", "Nourriture");
        Add("🍓", "fraise", "Nourriture");
        Add("🍒", "cerise", "Nourriture");
        Add("🍑", "pêche", "Nourriture");
        Add("🍍", "ananas", "Nourriture");
        Add("🥥", "noix de coco", "Nourriture");
        Add("🥑", "avocat", "Nourriture");
        Add("🍔", "burger hamburger", "Nourriture");
        Add("🍟", "frites", "Nourriture");
        Add("🍕", "pizza", "Nourriture");
        Add("🌭", "hot dog saucisse", "Nourriture");
        Add("🥪", "sandwich", "Nourriture");
        Add("🌮", "taco mexicain", "Nourriture");
        Add("🌯", "burrito", "Nourriture");
        Add("🥗", "salade repas sain", "Nourriture");
        Add("🍝", "pâtes spaghetti", "Nourriture");
        Add("🍜", "nouilles ramen", "Nourriture");
        Add("🍣", "sushi poisson riz japonais", "Nourriture");
        Add("🍦", "glace cornet", "Nourriture");
        Add("🍰", "gâteau dessert pâtisserie", "Nourriture");
        Add("🎂", "gâteau anniversaire bougies", "Nourriture");
        Add("🍫", "chocolat tablette", "Nourriture");
        Add("🍿", "popcorn cinéma", "Nourriture");
        Add("☕", "café tasse chaud matin espresso", "Nourriture");
        Add("🍵", "thé matcha", "Nourriture");
        Add("🍺", "bière chope fête apéro", "Nourriture");
        Add("🍷", "vin rouge verre", "Nourriture");
        Add("🥂", "trinquer champagne fête bravo", "Nourriture");

        // Activités & Voyages
        Add("⚽", "football ballon foot match", "Activités");
        Add("🏀", "basketball panier", "Activités");
        Add("🎾", "tennis raquette", "Activités");
        Add("🏐", "volleyball", "Activités");
        Add("🏉", "rugby ballon", "Activités");
        Add("🎮", "jeu vidéo manette console gaming", "Activités");
        Add("🎲", "dé jeu de société hasard", "Activités");
        Add("🎯", "cible fléchette but objectif", "Activités");
        Add("🚗", "voiture auto voyage route", "Voyages");
        Add("🚕", "taxi", "Voyages");
        Add("🚌", "bus transport", "Voyages");
        Add("🚄", "train grande vitesse tgv", "Voyages");
        Add("✈️", "avion voyage vol aéroport vacances", "Voyages");
        Add("🚀", "fusée espace décollage départ", "Voyages");
        Add("⛵", "bateau voilier mer", "Voyages");
        Add("🏖️", "plage mer parasol été vacances", "Voyages");
        Add("⛺", "tente camping nature", "Voyages");
        Add("🏠", "maison chez soi foyer", "Voyages");

        // Objets & Symboles
        Add("📱", "smartphone téléphone portable mobile", "Objets");
        Add("💻", "ordinateur pc portable laptop tech", "Objets");
        Add("📷", "appareil photo photographie", "Objets");
        Add("💡", "ampoule idée lumière", "Objets");
        Add("📦", "colis boîte paquet carton livraison", "Objets");
        Add("🎉", "fête confettis bravo anniversaire célébration", "Symboles");
        Add("✨", "étoiles étincelles magique brille", "Symboles");
        Add("🔥", "feu flamme chaud tendance buzz", "Symboles");
        Add("💯", "100 cent parfait sans faute", "Symboles");
        Add("❤️", "coeur rouge amour affection", "Symboles");
        Add("🧡", "coeur orange", "Symboles");
        Add("💛", "coeur jaune amitié", "Symboles");
        Add("💚", "coeur vert nature", "Symboles");
        Add("💙", "coeur bleu", "Symboles");
        Add("💜", "coeur violet", "Symboles");
        Add("🖤", "coeur noir", "Symboles");
        Add("🤍", "coeur blanc pur", "Symboles");
        Add("💔", "coeur brisé peine rupture", "Symboles");
        Add("⭐", "étoile star favori note", "Symboles");
        Add("🌟", "étoile brillante éclat", "Symboles");
        Add("⚡", "éclair foudre énergie rapide", "Symboles");
        Add("✅", "coche verte validé fait ok succès", "Symboles");
        Add("❌", "croix rouge erreur non annulé faux", "Symboles");
    }

    private void Add(string emoji, string name, string category)
    {
        _allEmojis.Add(new EmojiItem(emoji, name, category));
    }
}
