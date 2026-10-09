using Godot;

namespace PassengerArchive;

/// <summary>
/// Hand-authored content. Every carriage is built differently on purpose (see docs/DESIGN.md):
///   1 — one talkable person + a played memory + a hidden rule (fiction)
///   2 — two people talking to each other, you only overhear and may interrupt (inspired)
///   3 — a social carriage: victim, witness, newspaper reader, liar, friendly killer (inspired)
///   4 — the hero's own carriage and memories (hero)
/// The hero's own killer (the "Попутник") is in carriages 1-3 in different clothes, always with the same tells.
/// </summary>
public static class Carriages
{
	public const string HeroPhrase = "Далі — то далі.";

	// Looks that must stay recognisable between carriage and memory.
	public static readonly Outfit KepkaLook = new(new Color(0.30f, 0.30f, 0.32f), Hat: true, HatColor: new Color(0.16f, 0.22f, 0.17f));
	public static readonly Outfit PopOriginal = new(new Color(0.14f, 0.14f, 0.16f), Hat: true, HatColor: new Color(0.09f, 0.09f, 0.10f), LongCoat: true, Collar: true);

	static readonly Vector3 ConductorSpot = new(-1.15f, 0f, -6.45f);

	static PersonData Conductor(Vector3 at, float yaw, LineSet[] talk = null) => new(
		Id: "conductor", Name: "Провідник", Row: 0, Side: 0, Slot: 0,
		Look: new Outfit(new Color(0.12f, 0.2f, 0.18f), Hat: true, HatColor: new Color(0.1f, 0.16f, 0.14f), Collar: true),
		Talk: talk ?? new[]
		{
			// He never explains and never lies.
			new LineSet(new[]
			{
				new Line("— Куди їде потяг?\n— Далі."),
				new Line("— Хто ці люди?\n— Пасажири."),
				new Line("— Чому вони тут?\n— А чому ти?"),
				new Line("— Скільки ще?\n— Далі."),
			}, Loop: true),
		},
		StandAt: at, StandYaw: yaw);

	public static readonly CarriageData[] All =
	{
		// ═════════════ 1 · one person, a memory, a hidden rule ═════════════
		new CarriageData(
			Title: "ВАГОН 1",
			Kind: StoryKind.Fiction,
			CardNote: "Вигадана історія. Будь-які збіги випадкові.",
			RealWorld: null,
			DeadLamps: new[] { 1, 3 },
			People: new[]
			{
				Conductor(ConductorSpot, Mathf.Pi),

				new PersonData("viktor", "Віктор", Row: 3, Side: -1, Slot: 1,
					Look: new Outfit(new Color(0.32f, 0.22f, 0.15f)),
					Talk: new[]
					{
						new LineSet(new[]
						{
							new Line("Ви теж до кінцевої?"),
							new Line("Не знаю. Я просто сів. Двадцять років в одній конторі, додому — пішки, тут недалеко."),
							new Line("Зупинка, ліхтарі, під'їзд. Я міг би пройти це із заплющеними очима."),
							new Line("Так, мабуть, і ходив. Хочете побачити? Просто дивіться.", Fx: "memory:route"),
						}),
						new LineSet(new[]
						{
							new Line("Тепер ви знаєте, як це було."),
							new Line("Я пробував пригадати, де саме... Нічого не змінити."),
							new Line("Їдьте далі."),
						}, Requires: "story_done"),
					}),

				// Killer of Viktor. Nothing about him is obvious — he just sits. Same cap, same tapping hand as in the memory.
				new PersonData("kepka", "Чоловік у кепці", Row: 5, Side: +1, Slot: 0, Look: KepkaLook,
					Talk: new[] { new LineSet(new[] { new Line("Що?"), new Line("Нічого."), new Line("Я просто їду.") }) },
					Idle: Gesture.Tap, IsKiller: true),

				// The hero's own killer. Silent, near the back door.
				new PersonData("pop", "Чоловік у довгому пальті", Row: 7, Side: -1, Slot: 0, Look: PopOriginal,
					Talk: new[] { new LineSet(new[] { new Line(HeroPhrase) }) },
					Idle: Gesture.Sleeve, IsHeroKiller: true),

				new PersonData("bg1", "", Row: 1, Side: +1, Slot: 1,
					Look: new Outfit(new Color(0.3f, 0.18f, 0.2f), Scarf: true, ScarfColor: new Color(0.55f, 0.5f, 0.3f))),
				new PersonData("bg2", "", Row: 2, Side: -1, Slot: 0, Look: new Outfit(new Color(0.2f, 0.28f, 0.4f))),
			},
			Rule: new RuleDef(
				Kind: RuleKind.PhoneRing,
				Reason: "Ти відповів на дзвінок.",
				VictimName: "Віктором",
				Method: "удар ножем у темному під'їзді",
				KillText: "Слухавка мовчить. Позаду — кроки. Ти не встигаєш обернутись.",
				KillerId: "kepka")),

		// ═════════════ 2 · two people talking; you overhear ═════════════
		new CarriageData(
			Title: "ВАГОН 2",
			Kind: StoryKind.Inspired,
			CardNote: "Натхненна документально зафіксованими випадками. Імена та деталі змінено.",
			// PLACEHOLDER text: replace with sourced material before release.
				RealWorld: "Історії про нічні підвезення незнайомців трапляються в хроніках різних країн. Переважна більшість підвезень закінчується добре — тому ознаки небезпеки так легко пропустити: порожня дорога, ніч, мокрий одяг, і ніхто не знає, де ви.",
			StuckWorld: true,
			DeadLamps: new[] { 0 },
			People: new[]
			{
				Conductor(ConductorSpot, Mathf.Pi),

				new PersonData("galyna", "Галина", Row: 3, Side: +1, Slot: 0,
					Look: new Outfit(new Color(0.12f, 0.12f, 0.14f), Scarf: true, ScarfColor: new Color(0.35f, 0.35f, 0.38f)),
					Talk: new[]
					{
						new LineSet(new[] { new Line("Не треба. Дякую, що підійшли."), new Line("Ви його не знали. І добре.") }),
						new LineSet(new[] { new Line("Він був хорошою людиною. Це найгірше.") }, Requires: "story_done"),
					}),
				new PersonData("olya", "Оля", Row: 3, Side: +1, Slot: 1,
					Look: new Outfit(new Color(0.2f, 0.35f, 0.35f), Flowers: true),
					Talk: new[]
					{
						new LineSet(new[] { new Line("Я не знаю, що сказати. Тут нічого не скажеш.") }),
						new LineSet(new[] { new Line("Я хотіла, щоб квіти були від нас обох.") }, Requires: "story_done"),
					}),

				// The hero's killer, now friendly: sits with a newspaper, helps with the stuck door.
				new PersonData("pop", "Чоловік із газетою", Row: 6, Side: -1, Slot: 0,
					Look: new Outfit(new Color(0.42f, 0.38f, 0.3f), Paper: true),
					Talk: new[]
					{
						new LineSet(new[]
						{
							new Line("Ви куди їдете?"),
							new Line("Я теж не пам'ятаю. Забавно, правда?"),
							new Line(HeroPhrase),
						}),
					},
					Idle: Gesture.Sleeve, IsHeroKiller: true),

				new PersonData("bg1", "", Row: 1, Side: -1, Slot: 1, Look: new Outfit(new Color(0.25f, 0.3f, 0.28f), Hat: true, HatColor: new Color(0.2f, 0.2f, 0.2f))),
				new PersonData("bg2", "", Row: 5, Side: +1, Slot: 1, Look: new Outfit(new Color(0.35f, 0.25f, 0.3f))),
			},
			Overheard: new[]
			{
				new Line("Я принесла гвоздики. Він же любив гвоздики.", Speaker: "Оля"),
				new Line("Любив. Дарити тільки не вмів. Казав — гроші на вітер.", Speaker: "Галина"),
				new Line("Ти була там? Після?", Speaker: "Оля"),
				new Line("Один раз. Там вітер. Постояла й поїхала.", Speaker: "Галина"),
				new Line("Він повертався пізно. Дорога порожня, дощ. Подзвонив: «Підвезу одного, стоїть на узбіччі, весь мокрий».", Speaker: "Галина"),
				new Line("І ти нічого не сказала?", Speaker: "Оля"),
				new Line("Що я мала сказати? Що мені страшно? Він би розсміявся. «Люди ж».", Speaker: "Галина"),
				new Line("А потім телефон мовчав. До ранку. Машину знайшли за містом.", Speaker: "Галина"),
				new Line("Галю...", Speaker: "Оля"),
				new Line("Він ніколи не залишав людей на дорозі. Для нього це було, як дихати.", Speaker: "Галина"),
				new Line("Я досі думаю, що могла зробити щось інакше.", Fx: "done", Speaker: "Галина"),
			},
			OverheardAt: new Vector3(1.1f, 0f, -0.8f),
			DoorStuck: true,
			HelperScript: new[]
			{
				new Line("Заклинило? Дайте я.", Speaker: "Чоловік із газетою"),
				new Line("Ви куди їдете?", Speaker: "Чоловік із газетою"),
				new Line("Теж не знаєте? Буває.", Speaker: "Чоловік із газетою"),
				new Line(HeroPhrase, Fx: "unstick", Speaker: "Чоловік із газетою"),
			}),

		// ═════════════ 3 · a social carriage: five voices, contradicting versions ═════════════
		new CarriageData(
			Title: "ВАГОН 3",
			Kind: StoryKind.Inspired,
			CardNote: "Натхненна реальними випадками. Деталі змінено.",
			// PLACEHOLDER text: replace with sourced material before release.
				RealWorld: "У багатьох задокументованих випадках зникнення людину бачили востаннє в компанії того, кому вона довіряла або кого не хотіла образити. Незручність відмовити часто сильніша за інстинкт. Відмовитись можна завжди — навіть якщо це здається грубим.",
			DeadLamps: new[] { 2 },
			People: new[]
			{
				Conductor(ConductorSpot, Mathf.Pi),

				new PersonData("solomiya", "Соломія", Row: 2, Side: -1, Slot: 1,
					Look: new Outfit(new Color(0.22f, 0.36f, 0.3f), Scarf: true, ScarfColor: new Color(0.7f, 0.65f, 0.4f)),
					Talk: new[]
					{
						new LineSet(new[]
						{
							new Line("Я просто хотіла дістатися додому."),
							new Line("П'ятниця. Пізно. Автобус не прийшов."),
							new Line("Компанія розходилась. Я не хотіла здіймати галас."),
							new Line("Один сказав: «Темно. Давайте я вас проведу»."),
							new Line("Я погодилась. Мені здалось, це ввічливо."),
							new Line("Ви вже знаєте, що буде далі. Не намагайтесь мене врятувати. Ви не для цього тут.", Fx: "done"),
						}),
						new LineSet(new[] { new Line("Йдіть. Вам далі.") }, Requires: "story_done"),
					}),

				new PersonData("hanna", "Ганна", Row: 4, Side: +1, Slot: 1,
					Look: new Outfit(new Color(0.35f, 0.3f, 0.22f), Paper: true),
					Talk: new[]
					{
						new LineSet(new[]
						{
							new Line("Ви читали? Знову на пустирі за вокзалом."),
							new Line("Пишуть — дівчину. Грабіжник, кажуть. Ззаду підійшов."),
							new Line("Господи, а ми їздимо цими потягами щодня."),
							new Line("Один чоловік мені сказав: «Далі — то далі». Так спокійно сказав."),
						}),
					}),

				new PersonData("bohdan", "Богдан", Row: 5, Side: -1, Slot: 0,
					Look: new Outfit(new Color(0.25f, 0.25f, 0.3f)),
					Talk: new[]
					{
						new LineSet(new[]
						{
							new Line("Я бачив її з чоловіком. Привітний такий, у світлому светрі."),
							new Line("Що мені було робити? Втручатись? Я нічого не міг зробити."),
							new Line("Навіть обличчя не розгледів. Світло там було погане."),
						}),
					}),

				new PersonData("levko", "Левко", Row: 6, Side: +1, Slot: 0,
					Look: new Outfit(new Color(0.4f, 0.28f, 0.2f), Collar: true),
					Talk: new[]
					{
						new LineSet(new[]
						{
							new Line("Нічого я не бачив. Вона сама пішла. Я того вечора вдома був."),
							new Line("Чого ви так дивитесь? Вдома, кажу."),
							new Line("Спитайте в нього. Він усе знає."),
						}),
					}),

				// Killer of Solomiya. Friendly. Will offer to guide you when the lights go out.
				new PersonData("taras", "Тарас", Row: 1, Side: -1, Slot: 0,
					Look: new Outfit(new Color(0.55f, 0.55f, 0.52f)),
					Talk: new[]
					{
						new LineSet(new[]
						{
							new Line("Добрий вечір. Місця тут вистачає."),
							new Line("Тут усяке розповідають. Не слухайте."),
						}),
						new LineSet(new[]
						{
							new Line("Я завжди хотів допомогти. Такі, як вона, ходять самі, довірливі."),
							new Line("Вона сказала «ні» біля арки. Тоді вже було пізно казати «так»."),
							new Line("Не бійтесь. Ви ж не пішли зі мною. Поки що все добре."),
						}, Requires: "offer_declined"),
					},
					IsKiller: true),

				// The hero's killer, standing by the door.
				new PersonData("pop", "Чоловік біля дверей", Row: 0, Side: 0, Slot: 0,
					Look: new Outfit(new Color(0.36f, 0.26f, 0.18f), Collar: true),
					Talk: new[] { new LineSet(new[] { new Line(HeroPhrase) }) },
					Idle: Gesture.Sleeve, StandAt: new Vector3(1.15f, 0f, -6.35f), StandYaw: 0f, IsHeroKiller: true),
			},
			Rule: new RuleDef(
				Kind: RuleKind.OfferGuide,
				Reason: "Ти пішов за ним.",
				VictimName: "Соломією",
				Method: "удар по голові на пустирі за вокзалом",
				KillText: "Темно. Рука лягає на плече. «Ну, ходімо».",
				KillerId: "taras")),

		// ═════════════ 4 · the hero's own carriage ═════════════
		new CarriageData(
			Title: "ТВІЙ ВАГОН",
			Kind: StoryKind.Hero,
			CardNote: "",
			RealWorld: null,
			DeadLamps: new[] { 1, 3 },
			People: new[]
			{
				Conductor(new Vector3(0f, 0f, -1.5f), Mathf.Pi, new[]
				{
					new LineSet(new[]
					{
						new Line("Ти довго йшов до цього вагона."),
						new Line("Розкажи свою історію.", Fx: "memory:depart"),
					}),
					new LineSet(new[]
					{
						new Line("Ти не вибирав цей потяг. Він вибрав момент."),
						new Line("Станція.", Fx: "memory:arrive"),
					}, Requires: "mem_depart"),
				}),
			}),
	};

	public static PersonData Find(CarriageData c, string id)
	{
		foreach (var p in c.People) if (p.Id == id) return p;
		return null;
	}
}
