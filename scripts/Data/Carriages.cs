using Godot;

namespace PassengerArchive;

/// <summary>Hand-authored content for the prototype carriages. Replace/extend freely.</summary>
public static class Carriages
{
	public static readonly CarriageData[] All =
	{
		// ───────────── Carriage I ─────────────
		new CarriageData(
			Title: "ВАГОН І",
			Rule: "Не відчиняй чужу валізу.",
			VictimName: "чоловіка у сірому пальті",
			VictimMethod: "задушений червоним шарфом",
			KillDescription: "Шарф затягується на твоїй шиї. Вузол — зав'язаний лівою рукою.",
			VictimRow: 3, VictimSide: -1, VictimSlot: 0,
			VictimCoat: new Color(0.35f, 0.35f, 0.37f),
			VictimScarf: true,
			VictimWet: false,
			VictimExamine: new[]
			{
				new Line("Сіре пальто. Червоний шарф затягнуто надто міцно."),
				new Line("Вузол зав'язано з лівого боку. Лівою рукою.", "Вузол на шарфі зав'язано лівою рукою."),
				new Line("Він уже нічого не скаже."),
			},
			Prop: RuleProp.Suitcase,
			PropPos: new Vector3(-1.25f, 2.1f, 1.0f),
			StuckWorld: false,
			DeadLamps: new[] { 1, 3 },
			MinClues: 3,
			Passengers: new[]
			{
				new PassengerData("ninel", "Нінель", 2, +1, 0, new Color(0.35f, 0.18f, 0.2f), false, new[]
				{
					new Line("Ви теж до кінцевої?"),
					new Line("Я їду давно. Не рахую вже, відколи."),
					new Line("Він ще вчора був живий. Сидів тут і мовчав. А тоді згасло світло — і хтось тихо поправляв рукав. Ніби звик.",
						"Нінель: коли гасло світло, хтось поправляв рукав."),
					new Line("Більше нічого не скажу. Мене вони теж слухають."),
				}),
				new PassengerData("maksym", "Максим", 5, -1, 1, new Color(0.2f, 0.28f, 0.4f), false, new[]
				{
					new Line("Що? А... пробачте. Батарейки в касеті сіли."),
					new Line("Уночі клацнула застібка валізи на полиці. Я не дивився, хто.",
						"Максим: уночі клацнула застібка валізи на полиці."),
					new Line("Кажуть, її краще не чіпати."),
				}),
				new PassengerData("rita", "Рита", 4, +1, 1, new Color(0.3f, 0.3f, 0.2f), false, new[]
				{
					new Line("Не дивіться на нього так довго."),
					new Line("Шарф на ньому — не його. Він носив сіре, тільки сіре."),
					new Line("А той, що ззаду, весь час мерзне. Тільки шарфа в нього нема.",
						"Рита: пасажир ззаду мерзне, але шарфа не має."),
				}),
				new PassengerData("ilko", "Ілько", 6, +1, 0, new Color(0.22f, 0.2f, 0.17f), true, new[]
				{
					new Line("Сідайте, місця вистачить."),
					new Line("Я не пам'ятаю, де сів. Не пам'ятаю — і не питаю."),
					new Line("Холодно тут, правда? *поправляє рукав*",
						"Ілько поправляє рукав — так, як описувала Нінель."),
					new Line("Квиток? Ось, дивіться. *подає його лівою рукою*",
						"Ілько тримає квиток лівою рукою."),
					new Line("Ви надто багато питаєте."),
				}, Hat: true, HatColor: new Color(0.12f, 0.1f, 0.09f), SleeveHabit: true),
			}),

		// ───────────── Carriage II ─────────────
		new CarriageData(
			Title: "ВАГОН ІІ",
			Rule: "Не відчиняй вікно.",
			VictimName: "жінку у мокрій сукні",
			VictimMethod: "втоплена — легені повні води",
			KillDescription: "Вода піднімається у горлі. Вікно зачинене. Вагон сухий. Ти тонеш.",
			VictimRow: 2, VictimSide: +1, VictimSlot: 1,
			VictimCoat: new Color(0.15f, 0.22f, 0.35f),
			VictimScarf: false,
			VictimWet: true,
			VictimExamine: new[]
			{
				new Line("Сукня мокра до нитки. Волосся — теж."),
				new Line("Підлога під нею суха. Скло не запотіло.", "Жінку втопили в сухому вагоні."),
				new Line("На вустах — солонуватий присмак. Море?"),
			},
			Prop: RuleProp.Window,
			PropPos: new Vector3(1.72f, 1.45f, 0.8f),
			StuckWorld: true,
			DeadLamps: new[] { 0, 4 },
			MinClues: 2,
			Passengers: new[]
			{
				new PassengerData("ostap", "Остап", 1, -1, 1, new Color(0.25f, 0.3f, 0.28f), false, new[]
				{
					new Line("Я лікар. Колись був. Тепер — просто пасажир."),
					new Line("Хтось виходив у тамбур і повернувся мокрий по коліна. Хоч за вікном — жодної калюжі.",
						"Остап: хтось повернувся з тамбура мокрий по коліна."),
					new Line("Будинок за вікном. Я бачу його вже годину."),
				}),
				new PassengerData("zoya", "Зоя", 5, +1, 0, new Color(0.4f, 0.22f, 0.22f), false, new[]
				{
					new Line("Тихіше. Тут усі слухають."),
					new Line("Дід із парасолею — він її жодного разу не розкривав. А з неї крапає.",
						"Зоя: з парасолі діда крапає, хоч він її не розкривав."),
					new Line("Не питайте, куди ми їдемо. Я питала."),
				}),
				new PassengerData("starii", "Старий із парасолею", 4, -1, 0, new Color(0.18f, 0.18f, 0.2f), true, new[]
				{
					new Line("Дощу немає. Але парасоля — звичка."),
					new Line("Вона просила відчинити вікно. Я сказав — не треба."),
					new Line("Чого ви на мої черевики дивитесь?", "Під ногами старого — калюжа в сухому вагоні."),
					new Line("Сидіть тихо. Так краще для всіх."),
				}, Hat: true, HatColor: new Color(0.1f, 0.1f, 0.14f), Puddle: true),
			}),
	};
}
