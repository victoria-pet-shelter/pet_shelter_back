private int ResolveSpeciesId(string? text)
{
    if (string.IsNullOrWhiteSpace(text))
        return 0;

    string lower = text.ToLower();

    // 🐶 Dogs
    if (Regex.IsMatch(lower, @"\b(dog|suņi|sunītis|suns|такса|щенок|пёс|собака|шпиц|бульдог)\b"))
        return 1;

    // 🐱 Cats
    if (Regex.IsMatch(lower, @"\b(cat|kaķis|kaķītis|кот|кошка|кошечка|котёнок|meinkūns)\b"))
        return 2;

    // 🐰 Rabbits
    if (Regex.IsMatch(lower, @"\b(rabbit|truši|кролик|заяц|trusītis|крольчиха)\b"))
        return 3;

    // 🐦 Birds
    if (Regex.IsMatch(lower, @"\b(bird|putns|попугай|канарейка|голубь)\b"))
        return 4;

    // 🐹 Rodents
    if (Regex.IsMatch(lower, @"\b(hamster|rat|mouse|крыс|мыш|хомяк|žurka|pele|kāmītis)\b"))
        return 5;

    // 🐢 Reptiles
    if (Regex.IsMatch(lower, @"\b(reptile|ķirzaka|змея|ящерица|чешуя|bruņurupucis|черепаха)\b"))
        return 6;

    // 🐴 Horses
    if (Regex.IsMatch(lower, @"\b(horse|zirgs|пони|лошадь|ponijs)\b"))
        return 7;

    // 🐟 Fish
    if (Regex.IsMatch(lower, @"\b(fish|zivs|рыбка|рыба|zivtiņa)\b"))
        return 8;

    return 0;
}
