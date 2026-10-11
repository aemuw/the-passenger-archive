using Godot;

namespace PassengerArchive;

/// <summary>
/// Hand-authored content. Every carriage is built differently on purpose (see docs/DESIGN.md):
///   1 — one talkable person + a played memory + a hidden rule (fiction)
///   2 — two people talking to each other: you overhear, may interrupt, and one of them lets you see her imagined memory (inspired)
///   3 — a social carriage: victim, witness, newspaper reader, liar, friendly killer (inspired)
///   4 — the hero's own carriage and memories (hero)
/// The hero's own killer (Максим, "the seller") is in carriages 1–3 in different clothes, always with the same tells.
/// Why the hero is on this train: he did not wait for Marta (05:10, together) — he answered a stranger's ad for her late
/// father's camera (a «Зеніт»): night, cash, alone, "only tonight". Marta named every red flag in a voicemail. He didn't listen.
///
/// Dialogue: nodes with options. What the hero picks changes what people say (flags), and some choices
/// are remembered for the whole run ("k:…") — the man who rides with you quotes them at the very end.
/// </summary>
public static class Carriages
{
	/// <summary>His line. Said to Hanna in carriage 3, to the player at the door, and at the very end.</summary>
	public const string HeroPhrase = "До кінця — то до кінця.";

	// Looks that must stay recognisable between carriage and memory.
	public static readonly Outfit KepkaLook = new(new Color(0.30f, 0.30f, 0.32f), Hat: true, HatColor: new Color(0.16f, 0.22f, 0.17f), Beard: true, Build: 1.05f);
	public static readonly Outfit PopOriginal = new(new Color(0.14f, 0.14f, 0.16f), Hat: true, HatColor: new Color(0.09f, 0.09f, 0.10f), LongCoat: true, Collar: true, Skin: new Color(0.62f, 0.5f, 0.43f));
	public static readonly Outfit TarasLook = new(new Color(0.55f, 0.55f, 0.52f), Hair: new Color(0.55f, 0.45f, 0.28f), Skin: new Color(0.62f, 0.5f, 0.42f));
	public static readonly Outfit HitchLook = new(new Color(0.10f, 0.11f, 0.13f), Hat: true, HatColor: new Color(0.07f, 0.08f, 0.09f), Collar: true, Faceless: true);

	// ───────────── tiny authoring helpers ─────────────
	static Line L(string text, string fx = null, string who = null) => new(text, fx, who);
	static Line[] S(params string[] t)
	{
		var r = new Line[t.Length];
		for (int i = 0; i < t.Length; i++) r[i] = new Line(t[i]);
		return r;
	}
	static readonly Line[] None = new Line[0];
	static DNode N(string id, Line[] say, Opt[] opts = null, string next = null, string set = null, string fx = null, string ifNeed = null, string ifTo = null)
		=> new(id, say, opts, next, set, fx, ifNeed, ifTo);
	static Opt O(string text, string to = null, string need = null, string unless = null, string set = null, string fx = null)
		=> new(text, to, need, unless, set, fx);
	static Entry E(string node, string need = null, string unless = null) => new(node, need, unless);
	static Convo C(Entry[] entries, params DNode[] nodes) => new(entries, nodes);
	static PersonData BG(string id, int row, int side, int slot, Outfit look, Gesture idle = Gesture.None, params string[] barks)
		=> new(id, null, null, row, side, slot, look, Idle: idle, Barks: barks.Length > 0 ? barks : null);

	static readonly Vector3 ConductorSpot = new(-1.15f, 0f, -6.45f);

	// ───────────── the conductor: never explains, never lies ─────────────
	static readonly Convo ConductorTalk = C(
		new[] { E("hi"), E("back", need: "c_met") },
		N("hi", S("Слухаю."), next: "hub", set: "c_met"),
		N("back", S("Так?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Як вас звати?", "name", unless: "c_name"),
			O("Куди їде потяг?", "where", unless: "c_where"),
			O("Хто ці люди?", "who", unless: "c_who"),
			O("Чому вони тут?", "why", unless: "c_why"),
			O("Що в наступному вагоні?", "next", unless: "c_next"),
			O("Ви ніколи не брешете?", "lie", unless: "c_lie"),
			O("Нічого. Дякую."),
		}),
		N("name", S("Провідник.", "Імені тут ніхто не питає. Ви — перший за… не знаю, як це міряти."), set: "c_name", next: "hub"),
		N("where", S("Далі."), next: "hub", set: "c_where"),
		N("who", S("Пасажири."), next: "hub", set: "c_who"),
		N("why", S("А чому ти?"), next: "hub", set: "c_why"),
		N("next", S("Знаю."), set: "c_next", opts: new[] { O("То скажіть.", "next2"), O("Ясно.", "hub") }),
		N("next2", S("Це не моя історія."), next: "hub"),
		N("lie", S("Ні.", "Мені не потрібно. Достатньо не відповідати."), set: "c_lie", next: "hub"));

	static PersonData Conductor(Vector3 at, float yaw, Convo talk = null) => new(
		Id: "conductor", Name: "Провідник", Alias: "Провідник", Row: 0, Side: 0, Slot: 0,
		Look: new Outfit(new Color(0.12f, 0.2f, 0.18f), Hat: true, HatColor: new Color(0.1f, 0.16f, 0.14f), Collar: true, Hair: new Color(0.35f, 0.35f, 0.35f), Glasses: true),
		Dialog: talk ?? ConductorTalk, StandAt: at, StandYaw: yaw,
		Barks: new[] { "(не обертаючись) Квитків не перевіряю. Все одно всі вже куплені." });

	// ═════════════════════════════ CARRIAGE 1 · Viktor ═════════════════════════════
	// Viktor: a retired-looking accountant. Apologetic, counts things, jokes at his own expense, trails off.
	static readonly Convo ViktorTalk = C(
		new[] { E("first"), E("again", need: "v_met"), E("done", need: "story_done") },

		N("first", S("О. Ви теж без квитка? Я кишені перевіряв чотири рази. Чотири!", "Двадцять років у бухгалтерії — а де сів, не пам'ятаю."), opts: new[]
		{
			O("Тут вільно?", "free"),
			O("Ви в порядку?", "ok"),
			O("Куди їде цей потяг?", "where"),
		}),
		N("free", S("Вільно, вільно. Портфель приберу. Він у мене як собака: де я — там і він."), set: "v_met", next: "hub"),
		N("ok", S("Я? Ну… Знаєте, вперше за двадцять років не знаю, котра година.", "Це або відпустка, або щось серйозніше."), set: "v_met,v_ok", next: "hub"),
		N("where", S("Питайте провідника. Мені він сказав «далі». Я чекав продовження. Не було."), set: "v_met", next: "hub"),

		N("again", S("Так?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Як вас звати?", "name", unless: "v_name", set: "v_name"),
			O("Де ви живете?", "home", unless: "v_home"),
			O("Хто ці люди навколо?", "people", unless: "v_people"),
			O("Розкажіть, як ви добиралися додому.", "route", need: "v_home"),
			O("Не буду заважати."),
		}),
		N("name", S("Віктор. Віктор Степанович, але це вже для паперів.", "Тут просто Віктор."), fx: "know", next: "hub"),
		N("home", S("П'ятиповерхівка, третій під'їзд. Двадцять років одним шляхом: зупинка, чотири ліхтарі, арка, двір.", "Я їх рахував. Чотири ліхтарі. Один — миготів."), set: "v_home", next: "hub"),
		N("people", S("Он дівчина в шарфі — спить. Он той, у синьому, — теж, чи прикидається.", "А той, у кепці… бачите, барабанить? Весь цей час. Ніби рахує. А хто рахує — той чекає."),
			set: "v_people", opts: new[] { O("Ви його знаєте?", "kepka"), O("Ви просто втомились.", "tired"), O("Зрозуміло.", "hub") }),
		N("kepka", S("Не знаю. А здається, що знаю.", "Так буває, коли людину бачиш щодня, а в обличчя не дивишся."), next: "hub"),
		N("tired", S("Може. Але я двадцять років рахував чужі гроші.", "Я помічаю, коли хтось рахує щось інше."), next: "hub"),
		N("route", S("Та що там розповідати. Ті самі ліхтарі, та сама арка.", "Хочете — покажу. Тільки не втручайтесь. Просто йдіть позаду. Мені так легше."),
			opts: new[] { O("Покажіть.", "show"), O("Може, пізніше.", "hub") }),
		N("show", S("Дивіться."), fx: "memory:route"),

		N("done", S("Тепер ви знаєте, як це було.", "Я намагаюсь пригадати, де саме… Ні. Не пригадую. Усе виглядало звичайно."), opts: new[]
		{
			O("Мені шкода.", "sorry"),
			O("А чоловік у кепці?", "kepka2"),
			O("Їдьте спокійно."),
		}),
		N("sorry", S("Не треба. Просто їдьте далі. Тільки — ну, ви розумієте — без мене."), next: null),
		N("kepka2", S("Не питайте мене про нього. Я його більше не бачу.", "Він просто сидить. Ніби чекає, поки хтось візьме слухавку."), next: null));

	// Zakhar: flat, monosyllabic, mundane menace.
	static readonly Convo KepkaTalk = C(
		new[] { E("first"), E("seen", need: "mem_route") },
		N("first", S("Що?"), opts: new[]
		{
			O("Куди ви їдете?", "go"),
			O("Як вас звати?", "name", unless: "z_name", set: "z_name"),
			O("Ви часто так їздите?", "often"),
			O("Вибачте."),
		}),
		N("go", S("Куди й усі."), next: null),
		N("name", S("Захар.", "Вам що, анкету заповнювати?"), fx: "know", next: null),
		N("often", S("Не знаю. А ви?"), opts: new[] { O("Не пам'ятаю.", "mem"), O("Вперше.", "first_time") }),
		N("mem", S("Хто не пам'ятає — спокійно живе."), next: null),
		N("first_time", S("Всі колись вперше."), next: null),
		N("seen", S("Ви мене десь бачили?"), opts: new[]
		{
			O("На зупинці. Біля арки.", "stop"),
			O("Як вас звати?", "name", unless: "z_name", set: "z_name"),
			O("Ні. Вам здалося.", "no"),
		}),
		N("stop", S("Багато де стоять люди.", "Я просто їду."), next: null),
		N("no", S("Тоді не дивіться так довго."), next: null));

	// Maksym ("Макс"): easy, jokey, friendly to a fault. His name is a clue: it is the name of the "seller" in the hero's phone.
	static readonly Convo Pop1Talk = C(
		new[] { E("first"), E("again", need: "p1_hi") },
		N("first", S("О, і ви тут. Знаєте, я п'ять хвилин намагаюсь зрозуміти, чому в цьому вагоні вікна не запітніли.", "Всередині тепло, надворі мороз — а скло сухе. Незвично."),
			set: "k:met_pop1,p1_hi", next: "ask"),
		N("again", S("О, знову ви."), next: "ask"),
		N("ask", None, opts: new[]
		{
			O("Ви давно їдете?", "long", unless: "p1_long"),
			O("Як вас звати?", "name", unless: "p1_name"),
			O("Мені пора."),
		}),
		N("long", S("Та давно. Люблю дорогу. У дорозі всі щирі, помітили? Нікому нічого доводити.", "Мені головне — не зупинятись. " + HeroPhrase), set: "p1_long", next: "ask"),
		N("name", S("Максим. А ви — не кажіть, самі скажете, коли захочете.", "Я нікуди не поспішаю."), set: "p1_name,k:pop_named", fx: "know", next: "ask"));

	// ═════════════════════════════ CARRIAGE 2 · Halyna & Olya ═════════════════════════════
	// Halyna: clipped, ironic, hides in practical details (the tea is getting cold). Never sentimental on purpose.
	static readonly Convo GalynaTalk = C(
		new[] { E("first"), E("heard", need: "heard"), E("interrupt", need: "overheard_on", unless: "heard"), E("done", need: "story_done") },

		N("first", S("Ми знайомі? Ні? Тоді я нічого не зобов'язана пояснювати."), opts: new[]
		{
			O("Як вас звати?", "name", unless: "g_n", set: "g_n"),
			O("Вибачте."),
		}),
		N("name", S("Галина. Не Галя. Галя — це для тих, хто…", "(пауза) Галина."), fx: "know", next: null),
		N("interrupt", S("Не зараз, будь ласка."), opts: new[]
		{
			O("Вибачте, я випадково почув.", "sorry"),
			O("Я піду."),
		}),
		N("sorry", S("Нічого. Ви не перший, хто слухає.", "Тут усі слухають. А потім їдуть далі."), next: null),

		N("heard", S("А, ви все чули. Не вибачайтесь. Чужого горя не позичиш."), opts: new[]
		{
			O("Це був ваш чоловік?", "husband", unless: "g_h"),
			O("Ви були там після?", "wind", unless: "g_w"),
			O("Як вас звати?", "name2", unless: "g_n", set: "g_n"),
			O("Розкажіть, як усе було.", "tell", need: "g_h"),
			O("Не буду нав'язуватись."),
		}),
		N("name2", S("Галина. Ви ж чули — Оля мене Галею кличе. Їй можна."), fx: "know", next: "heard"),
		N("husband", S("Андрій. П'ятнадцять років.", "Він усіх підвозив: сусідів, студентів, діда з вокзалу. «Люди ж», — казав. Така в нього була відповідь на все."), set: "g_h", next: "heard"),
		N("wind", S("Один раз. Там вітер. Квіти кладуть — вітер їх розносить.", "Постояла й поїхала. Що там стояти?"), set: "g_w", next: "heard"),
		N("tell", S("Я не бачила. Але я так багато разів це уявляла, що, здається, була там.",
				"Хочете — дивіться. Тільки не питайте, який він був з обличчя. Я його не бачила, і моя уява теж відмовилась."),
			opts: new[] { O("Покажіть.", "show"), O("Може, не варто.", "heard") }),
		N("show", S("Тоді просто йдіть по дорозі."), fx: "memory:ride"),

		N("done", S("Він був хорошою людиною. Це найгірше."), opts: new[]
		{
			O("Що б ви йому сказали тоді?", "say"),
			O("Мені шкода."),
		}),
		N("say", S("Що мені страшно. Просто це. Одне речення.", "А я боялась, що він розсміється."), next: null));

	// Olya: talkative, nervous, over-explains, apologises, keeps hands busy.
	static readonly Convo OlyaTalk = C(
		new[] { E("first"), E("done", need: "story_done") },
		N("first", S("Ой, вибачте. Я просто… так, гвоздики. Не знаю, куди їх діти — тримаю, як квиток."), opts: new[]
		{
			O("Це ваші квіти?", "flowers"),
			O("Як вас звати?", "name", unless: "o_n", set: "o_n"),
			O("Я не хотів завадити."),
		}),
		N("name", S("Оля. Ольга Миколаївна, але… ну, ви розумієте, це зараз не головне."), fx: "know", next: "first"),
		N("flowers", S("Гвоздики. Він любив. Галя думає, що я їх для нього.", "А я — щоб було що тримати в руках, розумієте? Коли руки порожні — думаєш забагато."), opts: new[]
		{
			O("Ви були близькі?", "close"),
			O("Зрозуміло."),
		}),
		N("close", S("Ми десять років сиділи в одному кабінеті. Він мене щодня підвозив.", "Тієї ночі я була у відпустці. Я теж думаю: якби я була в машині…", "Ой. Не слухайте мене."), next: null),
		N("done", S("Я хотіла, щоб квіти були від нас обох. А Галя їх навіть у руки не взяла."), next: null));

	// Maksym again — warmer, he knows you. "You looked at me like I was a creditor."
	static readonly Convo Pop2Talk = C(
		new[] { E("first"), E("first_met", need: "k:met_pop1"), E("again", need: "p2_hi") },
		N("first", S("О, жива душа! Сідайте, місця вистачає. Довго їдете?"), set: "k:met_pop2,p2_hi", next: "ask"),
		N("first_met", S("О, знайоме обличчя! Ми, здається, перетиналися в тому вагоні?", "Ви ще дивились на мене так, ніби я кредитор. Довго їдете?"), set: "k:met_pop2,p2_hi", next: "ask"),
		N("ask", None, opts: new[]
		{
			O("Не пам'ятаю, звідки.", "mem", set: "k:said_dont_remember"),
			O("До кінцевої.", "end", set: "k:said_terminus"),
			O("А ви?", "you"),
		}),
		N("mem", S("Я теж. Забавно, правда? Сідаєш — і вже їдеш. Нікому не треба пояснювати, навіщо."), next: "hub"),
		N("end", S("До кінцевої! Ого. Люблю таких, хто знає, куди їде. Мені б так."), next: "hub"),
		N("you", S("Я? Я просто їду. Поруч. Люблю, коли є з ким."), next: "hub"),
		N("again", S("О, це ви. Ну що, влаштувались?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Що ви читаєте?", "paper", unless: "p2_paper"),
			O("Ви бачили, що тут було з тими двома?", "women", unless: "p2_women"),
			O("Як вас звати?", "name", unless: "p2_name"),
			O("Дякую, мені пора."),
		}),
		N("paper", S("Читаю? Так, звісно. Нічого цікавого. Одні й ті ж новини:", "хтось пішов не туди, хтось не тому повірив."), set: "p2_paper", next: "hub"),
		N("women", S("Бачив. Горе таке, що й сказати нічого. Я сів подалі — не люблю, коли чужі сльози бачать, що я бачу."), set: "p2_women", next: "hub"),
		N("name", S("Максим. Та кажіть просто «Макс», усі кажуть."), set: "p2_name,k:pop_named", fx: "know", next: "hub"),

		// Started by the game when the player tries the stuck door.
		N("door", S("Заклинило? Дайте я.", "Ви вже не вперше з такими дверима?"), opts: new[]
		{
			O("Дякую.", "door_thanks", set: "k:accepted_help"),
			O("Я сам.", "door_self", set: "k:refused_help"),
		}),
		N("door_thanks", S("Та нічого. Усі колись потребують, щоб хтось відчинив.", HeroPhrase), fx: "unstick"),
		N("door_self", S("Як хочете. Вона так не піддасться, але нехай.", "Ну от. Піддалась. Буває.", HeroPhrase), fx: "unstick"));

	// ═════════════════════════════ CARRIAGE 3 · five voices ═════════════════════════════
	// Solomiya: polite to a fault, apologises, dry humour about herself, a pharmacist who just finished a shift.
	static readonly Convo SolomiyaTalk = C(
		new[] { E("first"), E("again", need: "s_met"), E("done", need: "story_done") },

		N("first", S("Ой, вибачте, я, здається, зайняла ваше місце. Ви теж додому?"), opts: new[]
		{
			O("Я не знаю, куди їду.", "dunno"),
			O("Так, додому.", "home"),
			O("Ви в порядку?", "ok"),
		}),
		N("dunno", S("Тоді ми схожі. Тільки я знаю, куди їхала. Просто не доїхала."), set: "s_met", next: "hub"),
		N("home", S("Додому — це добре. Я теж їхала. У мене там кіт, він мене, мабуть, уже лає."), set: "s_met", next: "hub"),
		N("ok", S("Бувало краще. Я просто хотіла дістатися додому. Всього лише.", "Ви вибачте, що так сухо — я після зміни, в аптеці."), set: "s_met", next: "hub"),

		N("again", S("Так?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Як вас звати?", "name", unless: "s_name", set: "s_name"),
			O("Що сталося?", "what"),
			O("Не буду заважати."),
		}),
		N("name", S("Соломія. Мама казала — на честь бабусі.", "Бабуся б мене за цю історію вилаяла."), fx: "know", next: "hub"),
		N("what", S("П'ятниця. Пізно. Автобус не прийшов.", "Компанія розходилась. Я не хотіла здіймати галас."), opts: new[]
		{
			O("Треба було викликати таксі.", "taxi"),
			O("Що було далі?", "next"),
		}),
		N("taxi", S("Гроші були розписані по рахунках. Тоді здавалось — зайве.", "Знаєте, як воно: поки не трапилось, усе здається зайвим."), next: "next"),
		N("next", S("Один сказав: «Темно. Давайте я вас проведу».", "Я погодилась. Мені здалось, що відмовляти — якось… незручно.", "Я не можу вам просто розповісти. Хочете побачити?"),
			opts: new[] { O("Покажіть.", "show"), O("Не треба.", "no") }),
		N("no", S("Як хочете. Але ви вже тут."), next: "hub"),
		N("show", S("Тоді ходімо. Не поспішайте."), fx: "memory:walk"),

		N("done", S("Ви вже знаєте, що було далі.", "Не намагайтесь мене врятувати. Ви не для цього тут."), opts: new[]
		{
			O("Я нічого не міг зробити.", "nothing"),
			O("Хто це був?", "who"),
			O("Дякую, що розповіли."),
		}),
		N("nothing", S("Ні. Не могли. Але ви пам'ятатимете. Це вже щось."), next: null),
		N("who", S("Привітний. Це все, що я можу вам сказати.", "Привітний — і весь вечір поруч."), next: null));

	// Hanna: gossip, exclamations, tangents, the drama of someone who reads the news aloud to strangers.
	static readonly Convo HannaTalk = C(
		new[] { E("first") },
		N("first", S("Ви читали?! Ні? Господи, ви не повірите. Знову на пустирі за вокзалом!"), opts: new[]
		{
			O("Що там?", "what"),
			O("Як вас звати?", "name", unless: "h_n", set: "h_n"),
			O("Я не читаю новин."),
		}),
		N("name", S("Ганна Іванівна! Ну, Ганна.", "Я вам, між іншим, не просто так розповідаю — бачила, як ви на мою газету дивились."), fx: "know", next: "first"),
		N("what", S("Дівчину знайшли. Пишуть — грабіжник, ззаду підійшов.", "Господи, а ми цими потягами щодня!"), opts: new[]
		{
			O("Грабіжник? Так вирішили?", "police"),
			O("Вона була не сама?", "alone"),
			O("Страшно.", "scary"),
		}),
		N("police", S("Ну, так пишуть. Я ж не слідчий. Хоча дивно: сумку не взяли.", "Хто ж грабує і сумку не бере?"), next: "phrase"),
		N("alone", S("Не сама? Звідки ви знаєте? У газеті про це ні слова.", "Хоча… ой. А воно ж логічно."), next: "phrase"),
		N("scary", S("Ще й як! Я тепер з роботи тільки таксі. Ну, чи трамваєм. Але зі страхом."), next: "phrase"),
		N("phrase", S("А знаєте, що мені один чоловік у цьому потязі сказав? Спокійно так: «" + HeroPhrase + "»", "Мені аж по спині."), opts: new[]
		{
			O("Який чоловік?", "man"),
			O("Мало що люди кажуть."),
		}),
		N("man", S("У коричневому пальті. Біля дверей стояв. Спокійний такий.", "Усе одно я не люблю, коли так спокійно."), set: "k:hanna_pop", next: null));

	// Bohdan: defensive, hesitates, rationalises. A man who has been replaying one evening for weeks.
	static readonly Convo BohdanTalk = C(
		new[] { E("first") },
		N("first", S("Ви теж питати прийшли? Я вже всім сказав: нічого я не міг зробити."), opts: new[]
		{
			O("Що ви бачили?", "saw"),
			O("Чому ви не втрутились?", "why"),
			O("Як вас звати?", "name", unless: "b_n", set: "b_n"),
		}),
		N("name", S("Богдан. Богдан Петрович.", "Це що, допит?"), fx: "know", next: "first"),
		N("saw", S("Бачив її з чоловіком. Привітний такий, у світлому светрі.", "Він її за лікоть підтримував — ну, як підтримують. Я подумав — знайомі."), opts: new[]
		{
			O("Ви розгледіли його обличчя?", "face"),
			O("Чому ви не втрутились?", "why"),
		}),
		N("why", S("Що мені було робити? Втручатись? Сказати «ви її знаєте?» А якщо знає?", "Я ж не…"), opts: new[]
		{
			O("Могли хоча б подзвонити.", "call"),
			O("Я розумію.", "understand"),
		}),
		N("call", S("Хтось би подзвонив. Усі так думають.", "«Хтось подзвонить». Я теж так думав."), next: null),
		N("understand", S("Ні, не розумієте. Я й сам не розумію.", "Я собаку потім три дні не міг вигуляти. Стою на порозі — і не можу."), next: null),
		N("face", S("Світло там було погане. Нічого не розгледів.", "Лише светр. Світлий. Дорогий, певно."), next: null));

	// Levko: brusque, evasive, picks a fight to avoid answering.
	static readonly Convo LevkoTalk = C(
		new[] { E("first") },
		N("first", S("Чого вам? Нічого я не бачив. Вона сама пішла. Я того вечора вдома був."), opts: new[]
		{
			O("Я не питав про вечір.", "slip"),
			O("Хто тоді був?", "who"),
			O("Як вас звати?", "name", unless: "l_n", set: "l_n"),
		}),
		N("name", S("Левко. А вам навіщо? Ви що, з поліції?"), fx: "know", next: "first"),
		N("slip", S("Ну… я просто. Мало що люди плетуть.", "Чого ви так дивитесь? Вдома, кажу."), set: "l_slip", opts: new[] { O("Хто тоді був?", "who"), O("Гаразд.") }),
		N("who", S("Спитайте в нього. Він усе знає. Він і не таке бачив."), set: "l_pointed", opts: new[]
		{
			O("У кого?", "taras"),
			O("Не буду вас більше мучити."),
		}),
		N("taras", S("У нашого тямущого. Світлий светр. Він завжди знає, хто куди пішов — і всіх проводить."), next: null));

	// Taras: warm, flattering, smooth; every sentence is a little too kind. The villain who is pleasant company.
	static readonly Convo TarasTalk = C(
		new[] { E("first"), E("after", need: "offer_declined"), E("confess", need: "t_asked_story") },

		N("first", S("Добрий вечір! Місця тут вистачає. Гарне пальто, до речі, — зараз такого не шиють."), opts: new[]
		{
			O("Як вас звати?", "name", unless: "t_n", set: "t_n"),
			O("Ви тут давно?", "long"),
			O("Ви не знали дівчину в зеленому пальті?", "know"),
			O("Левко каже, що ви все знаєте.", "levko", need: "l_pointed"),
			O("Дякую."),
		}),
		N("name", S("Тарас. А ви — не кажіть, мені тут усі розповідають.", "Люди чомусь довіряють. Не знаю чому."), fx: "know", next: "first"),
		N("long", S("Від початку. Люблю потяги. Тут усі розповідають — як на сповіді.", "Не слухайте. Я шуткую."), next: null),
		N("know", S("Ні. Хто ж їх знає? Я з жінками обережний.", "У мене сестра така сама — довірлива. Я за неї переживаю."), next: null),
		N("levko", S("Левко? Вигадує. Він завжди вигадує.", "Він би і рідного батька виправдав. Або звинуватив."), next: null),

		N("after", S("Я завжди хотів допомогти людям. Такі, як вона, ходять самі, довірливі…"), opts: new[]
		{
			O("Розкажіть, як усе було.", "story"),
			O("Ви її вбили.", "accuse"),
			O("Я піду."),
		}),
		N("accuse", S("Я? Ні-ні. Я просто розповідаю історію. Тут усі розповідають."), next: "after"),
		N("story", S("Я чекав на зупинці. Просто стояв, нічого такого.", "Вона виходила з компанії сама. Я сказав: «Темно, давайте проведу». Вона посміхнулась.",
				"Ввічлива. Такі завжди ввічливі.", "Біля арки сказала «ні». Тихо. А я вже не міг повернути назад."),
			set: "t_asked_story", next: "story2"),
		N("story2", S("Не думайте, що я чудовисько. Я звичайна людина.", "У цьому й вся проблема."), opts: new[]
		{
			O("Чому ви мені це кажете?", "why"),
			O("(Мовчати.)"),
		}),
		N("why", S("Бо ви не пішли зі мною. Поки що все добре."), next: null),
		N("confess", S("Хочете ще раз послухати? Я не проти. Мені навіть приємно."), next: "story"),

		// Started by the game when the lights die, 35 s after you enter.
		N("offer", S("Ох, темно. Давайте я вас проведу до дверей. Я тут усе знаю."), opts: new[]
		{
			O("Дякую, ходімо.", "go"),
			O("Ні, я сам.", "no"),
			O("(Промовчати.)", "silent"),
		}),
		N("go", S("От і добре. Ходімо."), fx: "violate"),
		N("no", S("Як знаєте. Тоді сидіть тихо."), set: "offer_declined"),
		N("silent", S("Мовчите? Ну, не заважайте."), set: "offer_declined"));

	// Maksym at the door: courteous, a shade evasive — and he lies about the newspaper.
	static readonly Convo Pop3Talk = C(
		new[] { E("first") },
		N("first", S("Ви теж дивитесь на двері? Звичка. Двері — єдине, що тут чесне: або відчинені, або ні."), next: "ask"),
		N("ask", None, opts: new[]
		{
			O("Ви читали газету в сусідньому вагоні.", "lie", need: "k:met_pop2", unless: "p3_lie"),
			O("Як вас звати?", "name", unless: "p3_n"),
			O("Ви тут давно?", "long", unless: "p3_long"),
			O("Дякую."),
		}),
		N("lie", S("Газету? Ні. Ви, мабуть, сплутали. Тут усі схожі.", HeroPhrase), set: "p3_lie,k:caught_pop_lie", next: "ask"),
		N("name", S("Максим. Ми вже представлялись? Ні? Ну, тепер так."), set: "p3_n,k:pop_named", fx: "know", next: "ask"),
		N("long", S("Я їду. Як і ви."), set: "p3_long", next: "ask"));

	// ═════════════════════════════ CARRIAGE 4 · the hero ═════════════════════════════
	static readonly Convo HeroConductor = C(
		new[] { E("h1"), E("h2", need: "mem_depart") },
		N("h1", S("Ти довго йшов до цього вагона."), next: "ask"),
		N("ask", None, opts: new[]
		{
			O("Я нікуди не йшов. Я просто сів.", "sat", unless: "h_sat"),
			O("Хто той чоловік у пальті?", "pop", need: "k:met_pop2", unless: "h_pop"),
			O("Куди ми їдемо?", "where", unless: "h_where"),
			O("Я готовий.", "story"),
		}),
		N("sat", S("Ти сів. Це теж рішення."), set: "h_sat", next: "ask"),
		N("pop", S("Пасажир.", "Він завжди сідає там, де сідаєш ти."), set: "h_pop", next: "ask"),
		N("where", S("Туди, де ти вже був."), set: "h_where", next: "ask"),
		N("story", S("Розкажи свою історію."), fx: "memory:depart"),

		N("h2", S("Ти не вибирав цей потяг. Ти вибрав момент."), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Вона просила мене почекати.", "marta", need: "k:heard_marta", unless: "h_marta"),
			O("Що буде на станції?", "what", unless: "h_what"),
			O("Я готовий.", "arrive"),
		}),
		N("marta", S("Знаю.", "Усі, хто тут, колись когось не послухали."), set: "h_marta", next: "hub"),
		N("what", S("Те, що вже було."), set: "h_what", next: "hub"),
		N("arrive", S("Станція."), fx: "memory:arrive"));

	// ═════════════════════════════ the carriages ═════════════════════════════
	public static readonly CarriageData[] All =
	{
		// ───────────── 1 · one person, a memory, a hidden rule ─────────────
		new CarriageData(
			Title: "ВАГОН 1",
			Kind: StoryKind.Fiction,
			CardNote: "Вигадана історія. Будь-які збіги випадкові.",
			RealWorld: null,
			DeadLamps: new[] { 1, 3 },
			People: new[]
			{
				Conductor(ConductorSpot, Mathf.Pi),

				new PersonData("viktor", "Віктор", "Незнайомець у коричневому піджаку, що рахує ліхтарі за вікном",
					Row: 3, Side: -1, Slot: 1,
					Look: new Outfit(new Color(0.32f, 0.22f, 0.15f), Glasses: true, Bag: true, Hair: new Color(0.55f, 0.55f, 0.55f), Build: 0.94f),
					Dialog: ViktorTalk, Idle: Gesture.Fidget,
					Barks: new[] { "(тихо) Один, два, три, чотири… Чотири ліхтарі.", "(стискає портфель) Двадцять років… двадцять років.", "Вибачте, я задумався." }),

				// Killer of Viktor. Nothing about him is obvious — he just sits. Same cap, same tapping hand as in the memory.
				new PersonData("kepka", "Захар", "Незнайомець у зеленій кепці, що барабанить пальцями по коліну",
					Row: 5, Side: +1, Slot: 0, Look: KepkaLook, Dialog: KepkaTalk, Idle: Gesture.Tap, IsKiller: true,
					Barks: new[] { "(не дивлячись) Вам щось потрібно?", "Чого стоїте?" }),

				// The hero's own killer, near the back door. Cheerful. Straightens his sleeve.
				new PersonData("pop", "Максим", "Незнайомець у довгому пальті, що поправляє рукав",
					Row: 7, Side: -1, Slot: 0, Look: PopOriginal, Dialog: Pop1Talk, Idle: Gesture.Sleeve, IsHeroKiller: true,
					Barks: new[] { "(поправляє рукав) О, ви ще тут?", "Гарна погода для дороги, правда?" }),

				BG("bg1", 1, +1, 1, new Outfit(new Color(0.3f, 0.18f, 0.2f), Scarf: true, ScarfColor: new Color(0.55f, 0.5f, 0.3f), Hair: new Color(0.45f, 0.3f, 0.15f), Build: 0.9f), Gesture.Sleep,
					"(уві сні) …ще п'ять хвилин…"),
				BG("bg2", 2, -1, 0, new Outfit(new Color(0.2f, 0.28f, 0.4f), Beard: true, Build: 1.08f), Gesture.Window,
					"(не обертаючись) Гарна ніч. Нічого не видно."),
			},
			Rule: new RuleDef(
				Kind: RuleKind.PhoneRing,
				Reason: "Ти відповів на дзвінок.",
				VictimId: "viktor",
				VictimName: "Віктором",
				Method: "удар ножем у темному під'їзді",
				KillText: "Слухавка мовчить. Позаду — кроки. Лезо входить під ребра раніше, ніж ти встигаєш обернутись.",
				KillerId: "kepka")),

		// ───────────── 2 · two people talking; you overhear ─────────────
		new CarriageData(
			Title: "ВАГОН 2",
			Kind: StoryKind.Inspired,
			CardNote: "Натхненна документально зафіксованими випадками. Імена та деталі змінено.",
			// PLACEHOLDER text: replace with sourced material before release (see docs/REAL_STORIES.md).
			RealWorld: "Історії про нічні підвезення незнайомців трапляються в хроніках різних країн. Переважна більшість підвезень закінчується добре — тому ознаки небезпеки так легко пропустити: порожня дорога, ніч, мокрий одяг, і ніхто не знає, де ви.",
			StuckWorld: true,
			DeadLamps: new[] { 0 },
			People: new[]
			{
				Conductor(ConductorSpot, Mathf.Pi),

				new PersonData("galyna", "Галина", "Незнайома жінка в сірому шарфі, що дивиться повз усіх",
					Row: 3, Side: +1, Slot: 0,
					Look: new Outfit(new Color(0.12f, 0.12f, 0.14f), Scarf: true, ScarfColor: new Color(0.35f, 0.35f, 0.38f), Hair: new Color(0.35f, 0.33f, 0.33f), Build: 0.9f),
					Dialog: GalynaTalk, Idle: Gesture.Window,
					Barks: new[] { "(дивиться на чай) Холоне.", "Не стійте над душею, будь ласка." }),
				new PersonData("olya", "Оля", "Незнайомка з гвоздиками, яка не знає, куди їх діти",
					Row: 3, Side: +1, Slot: 1,
					Look: new Outfit(new Color(0.2f, 0.35f, 0.35f), Flowers: true, Hair: new Color(0.55f, 0.3f, 0.15f), Build: 0.92f),
					Dialog: OlyaTalk, Idle: Gesture.Fidget,
					Barks: new[] { "Ой, вибачте… я думала вголос.", "Він завжди казав: «Оля, не нервуй». А я нервую." }),

				// The hero's killer, now friendly: sits with a newspaper he never turns, helps with the stuck door.
				new PersonData("pop", "Максим", "Незнайомець із газетою, що не перегортає сторінок",
					Row: 6, Side: -1, Slot: 0, Look: new Outfit(new Color(0.42f, 0.38f, 0.3f), Paper: true, Skin: new Color(0.62f, 0.5f, 0.43f)),
					Dialog: Pop2Talk, Idle: Gesture.Sleeve, IsHeroKiller: true,
					Barks: new[] { "(не відриваючись від газети) Гарна сторінка. Другу годину дивлюсь.", "Сідайте, не лякайте мене." }),

				BG("bg1", 1, -1, 1, new Outfit(new Color(0.25f, 0.3f, 0.28f), Hat: true, HatColor: new Color(0.2f, 0.2f, 0.2f), Glasses: true), Gesture.Sleep),
				BG("bg2", 5, +1, 1, new Outfit(new Color(0.35f, 0.25f, 0.3f), Bag: true), Gesture.Window,
					"(тихо) Знову дощ."),
			},
			Overheard: new[]
			{
				L("Я принесла гвоздики. Він же любив гвоздики.", "flag:overheard_on", "@olya"),
				L("Любив. Дарити тільки не вмів. Казав — гроші на вітер.", null, "@galyna"),
				L("Ти була там? Після?", null, "@olya"),
				L("Один раз. Там вітер. Постояла й поїхала. Оль, ти ж знаєш.", "know:olya", "@galyna"),
				L("Він повертався пізно. Дорога порожня, дощ. Подзвонив: «Підвезу одного, стоїть на узбіччі, весь мокрий».", null, "@galyna"),
				L("І ти нічого не сказала?", null, "@olya"),
				L("Що я мала сказати? Що мені страшно? Він би розсміявся. «Люди ж».", null, "@galyna"),
				L("А потім телефон мовчав. До ранку. Машину знайшли за містом.", null, "@galyna"),
				L("Галю…", "know:galyna", "@olya"),
				L("Він ніколи не залишав людей на дорозі. Для нього це було, як дихати.", null, "@galyna"),
				L("Я досі думаю, що могла зробити щось інакше.", "flag:heard", "@galyna"),
			},
			OverheardAt: new Vector3(1.1f, 0f, -0.8f),
			DoorStuck: true),

		// ───────────── 3 · a social carriage: five voices, contradicting versions ─────────────
		new CarriageData(
			Title: "ВАГОН 3",
			Kind: StoryKind.Inspired,
			CardNote: "Натхненна реальними випадками. Деталі змінено.",
			// PLACEHOLDER text: replace with sourced material before release (see docs/REAL_STORIES.md).
			RealWorld: "У багатьох задокументованих випадках зникнення людину бачили востаннє в компанії того, кому вона довіряла або кого не хотіла образити. Незручність відмовити часто сильніша за інстинкт. Відмовитись можна завжди — навіть якщо це здається грубим.",
			DeadLamps: new[] { 2 },
			People: new[]
			{
				Conductor(ConductorSpot, Mathf.Pi),

				new PersonData("solomiya", "Соломія", "Незнайомка в зеленому пальті, що тримає сумку обома руками",
					Row: 2, Side: -1, Slot: 1,
					Look: new Outfit(new Color(0.22f, 0.36f, 0.3f), Scarf: true, ScarfColor: new Color(0.7f, 0.65f, 0.4f), Bag: true, Hair: new Color(0.08f, 0.06f, 0.05f), Build: 0.9f),
					Dialog: SolomiyaTalk, Idle: Gesture.Fidget,
					Barks: new[] { "Вибачте, я просто… (поправляє сумку).", "Ви стоїте, а я мовчу. Незручно, так?" }),

				new PersonData("hanna", "Ганна", "Незнайомка з газетою й виразом «ви не повірите»",
					Row: 4, Side: +1, Slot: 1,
					Look: new Outfit(new Color(0.35f, 0.3f, 0.22f), Paper: true, Glasses: true, Hair: new Color(0.62f, 0.55f, 0.4f), Build: 1.05f),
					Dialog: HannaTalk, Idle: Gesture.Fidget,
					Barks: new[] { "(шарудить газетою) Ви не повірите, що тут пишуть.", "Ой, господи, ну і темно." }),

				new PersonData("bohdan", "Богдан", "Незнайомець у сірому, що дивиться крізь стіну",
					Row: 5, Side: -1, Slot: 0, Look: new Outfit(new Color(0.25f, 0.25f, 0.3f), Beard: true, Hair: new Color(0.3f, 0.22f, 0.15f)),
					Dialog: BohdanTalk, Idle: Gesture.Window,
					Barks: new[] { "(дивиться крізь стіну) Я нічого не міг…", "Вибачте. Задумався." }),

				new PersonData("levko", "Левко", "Незнайомець у коричневому з піднятим коміром, що нікого не бачить",
					Row: 6, Side: +1, Slot: 0, Look: new Outfit(new Color(0.4f, 0.28f, 0.2f), Collar: true, Build: 1.12f),
					Dialog: LevkoTalk, Idle: Gesture.Tap,
					Barks: new[] { "(буркотить) Не стійте тут.", "Що, цікаво?" }),

				// Killer of Solomiya. Friendly. Offers to guide you when the lights go out.
				new PersonData("taras", "Тарас", "Незнайомець у світлому светрі, надто привітний для цього вагона",
					Row: 1, Side: -1, Slot: 0, Look: TarasLook, Dialog: TarasTalk, IsKiller: true,
					Barks: new[] { "(усміхається) Чудові люди тут їдуть, правда?", "Ви не мерзнете? Сідайте ближче." }),

				// The hero's killer, standing by the door.
				new PersonData("pop", "Максим", "Незнайомець біля дверей, що ніби теж чекає зупинки",
					Row: 0, Side: 0, Slot: 0, Look: new Outfit(new Color(0.36f, 0.26f, 0.18f), Collar: true, Skin: new Color(0.62f, 0.5f, 0.43f)),
					Dialog: Pop3Talk, Idle: Gesture.Sleeve, StandAt: new Vector3(1.15f, 0f, -6.35f), StandYaw: 0f, IsHeroKiller: true,
					Barks: new[] { "(поправляє рукав) Ще далеко.", "Зупинок тут немає. Ви ж знаєте." }),
			},
			Rule: new RuleDef(
				Kind: RuleKind.OfferGuide,
				Reason: "Ти пішов за ним.",
				VictimId: "solomiya",
				VictimName: "Соломією",
				Method: "удар по голові на пустирі за вокзалом",
				KillText: "Темно. Рука лягає на плече. «Ну, ходімо». Потім — тупий хруст, і підлога раптом опиняється біля обличчя.",
				KillerId: "taras")),

		// ───────────── 4 · the hero's own carriage ─────────────
		new CarriageData(
			Title: "ТВІЙ ВАГОН",
			Kind: StoryKind.Hero,
			CardNote: "",
			RealWorld: null,
			DeadLamps: new[] { 1, 3 },
			People: new[]
			{
				Conductor(new Vector3(0f, 0f, -1.5f), Mathf.Pi, HeroConductor),
			}),
	};

	public static PersonData Find(CarriageData c, string id)
	{
		foreach (var p in c.People) if (p.Id == id) return p;
		return null;
	}

	// ───────────── memory conversations (played inside MemoryScene) ─────────────
	/// <summary>Galyna's imagined last evening of her husband: a faceless man in the rain.</summary>
	public static readonly Convo HitchTalk = C(
		new[] { E("hi") },
		N("hi", S("Дякую, що зупинились. Годину тут стою, жоден не гальмує.", "Вибачте, що так, посеред дороги…"), opts: new[]
		{
			O("Куди вам?", "where"),
			O("Сідайте.", "sit"),
			O("Вибачте, я їду повз.", "pass"),
		}),
		N("where", S("Та тут, недалеко. Я скажу, де повернути.", "Тільки дозвольте, я весь мокрий — сидіння забруджу."), opts: new[]
		{
			O("Нічого страшного. Сідайте.", "sit"),
			O("Далеко ще?", "far"),
		}),
		N("far", S("Ні. Зовсім ні."), next: "sit"),
		N("pass", new[] { L("Ти хочеш проїхати повз. Але нога вже на гальмі, а рука — на ручці дверцят.", null, "—"), L("Це вже сталося.", null, "—") }, next: "sit"),
		N("sit", S("Дякую. Є ще люди, справді."), fx: "memend"));

	/// <summary>Solomiya's evening. Say "no" twice — the third time the word will not come out.</summary>
	public static readonly Convo WalkTalk = C(
		new[] { E("hi") },
		N("hi", S("Ох, темно. Давайте я вас проведу."), opts: new[]
		{
			O("Ні, дякую. Я зачекаю автобус.", "no1", set: "w1"),
			O("Добре.", "ok"),
		}),
		N("no1", S("Автобус? Він уже не прийде. Вісімка останній пройшла о пів на дванадцяту."), next: "hi2"),
		N("hi2", S("Та мені по дорозі. Правда."), opts: new[]
		{
			O("Ні, справді, не треба.", "no2", set: "w2"),
			O("Ну… гаразд.", "ok"),
		}),
		N("no2", S("Ну що ви. Я ж не кусаюсь.", "Невже ви мені не довіряєте?"), next: "hi3"),
		N("hi3", S("Ходімо. Там, де світло, — десять хвилин."), opts: new[]
		{
			O("Ні.", "no3"),
			O("Гаразд, ходімо.", "ok"),
		}),
		N("no3", new[]
		{
			L("Ти намагаєшся сказати «ні» втретє. Але на тебе дивляться — і слово застрягає, ніби ти зробила щось непристойне.", null, "—"),
			L("— Гаразд.", null, "Ти"),
		}, next: "ok"),
		N("ok", S("От і добре."), fx: "walkgo"));

	/// <summary>The arch. Whatever you say, it has already happened.</summary>
	public static readonly Convo ArchTalk = C(
		new[] { E("arch") },
		N("arch", S("Тут коротше."), opts: new[]
		{
			O("Ні. Я туди не піду.", "no"),
			O("(Промовчати.)", "no"),
		}),
		N("no", S("Он як."), fx: "memend"));

	/// <summary>
	/// The hero's phone at the station. The ad, the seller's messages, Marta's voicemail naming every red flag.
	/// Hearing it is remembered ("k:heard_marta") — the conductor and the killer both bring it up.
	/// </summary>
	public static readonly Convo HeroPhone = C(
		new[] { E("chat"), E("again", need: "ph_seen") },
		N("chat", new[]
		{
			L("Ну що, їдете? Я о 03:12 на кінцевій. Готівка, 3500.", null, "Максим · «Зеніт»"),
			L("Буду. Не продавайте нікому.", null, "Ти"),
			L("Тільки сам, добре? Мені потім ще з одним зустрічатись. І без зайвих розмов.", null, "Максим · «Зеніт»"),
		}, set: "ph_seen", next: "vm"),
		N("vm", new[]
		{
			L("Це я. Знову не береш.", null, "Марта · голосова пошта"),
			L("Слухай. Він не хоче вдень, не хоче в центрі. Готівка, ніч, сам, і «тільки сьогодні». Мені це не подобається.", null, "Марта · голосова пошта"),
			L("Мені не потрібен цей «Зеніт», чуєш? Тато б і так тебе полюбив. Почекай мене. Поїдемо разом о 05:10. Прошу.", null, "Марта · голосова пошта"),
		}, set: "k:heard_marta", next: "act"),
		N("act", None, opts: new[]
		{
			O("Передзвонити.", "call"),
			O("Написати: «Все буде добре».", "text"),
			O("Сховати телефон."),
		}),
		N("call", new[] { L("Довгі гудки. Не бере — мабуть, уже збирається.", null, "—") }),
		N("text", new[] { L("Надіслано. Дві галочки. Прочитала.", null, "—") }),
		N("again", new[] { L("Екран гасне.", null, "—") }));

	/// <summary>Marta's call at the terminus. Same phone sound that ended Viktor.</summary>
	public static readonly Convo MartaCall = C(
		new[] { E("hi") },
		N("hi", S("Нарешті! Ти де? Я тричі дзвонила."), opts: new[]
		{
			O("Я на кінцевій. Тут нікого.", "fine"),
			O("Пробач. Я не хотів, щоб він продав комусь іншому.", "sorry"),
			O("(Мовчати.)", "silent"),
		}),
		N("fine", S("Нікого? А він? Продавець?", "Ти ж не віддав йому гроші наперед?"), next: "mid"),
		N("sorry", S("Знаю. Для тебе це було про мене. Але мені потрібен не фотоапарат, а ти. Живий і нудний."), next: "mid"),
		N("silent", S("Алло? Ти тут? Я чую, що ти дихаєш."), next: "mid"),
		N("mid", S("Слухай. Стій там, де світло і є люди. Не йди нікуди з ним, поки я не приїду.", "Я вже виходжу. Дві години — і я там."), opts: new[]
		{
			O("Добре. Чекатиму.", "end", set: "k:promised_marta"),
			O("Я люблю тебе.", "love"),
		}),
		N("love", S("Я теж. Чуєш? Не відходь від ліхтаря."), next: "end2"),
		N("end", S("Обіцяєш?"), opts: new[] { O("Обіцяю.", "end2") }),
		N("end2", S("Тоді до зустрічі."), fx: "callend"));

	/// <summary>
	/// «Продавець». The man who has ridden with you since the first carriage — Максим. Every answer you gave him is remembered.
	/// </summary>
	public static readonly Convo KillerTalk = C(
		new[] { E("hi") },
		N("hi", S("Вибачте. Ви — по «Зеніт»?"), opts: new[]
		{
			O("Максим?", "m_known", need: "k:pop_named"),
			O("Ви… продавець?", "m_unknown", unless: "k:pop_named"),
			O("Де камера?", "cam"),
			O("Ви їхали цим потягом?", "train"),
		}),
		N("m_known", S("Ну так. Я ж казав у потязі: Максим. Ви тоді були такий задумливий."), next: "end"),
		N("m_unknown", S("А хто ж. Ми листувались. Ви такий, як я уявляв."), next: "end"),
		N("cam", S("У сумці. Тут темно — давайте під арку, там світліше."), next: "end"),
		N("train", S("Від першого вагона. Мені треба було переконатись, що ви приїдете самі.", "Ви ж самі, так?"), next: "end"),

		N("end", S("Знаєте, мені подобаються люди, які приходять самі. Це рідкість."), ifNeed: "k:said_dont_remember", ifTo: "e_mem", next: "e1b"),
		N("e_mem", S("Ви ж казали мені в потязі, що не пам'ятаєте, звідки їдете. А виявилось — за «Зенітом»."), next: "e2"),
		N("e1b", None, ifNeed: "k:said_terminus", ifTo: "e_term", next: "e2"),
		N("e_term", S("Ви казали, що їдете до кінцевої. Ну от, дісталися."), next: "e2"),
		N("e2", None, ifNeed: "k:heard_marta", ifTo: "e_marta", next: "last"),
		N("e_marta", S("Вам дзвонили. Тричі, я чув. Вона просила почекати? Розумна."), next: "last"),
		N("last", S("Ходімо під арку. Там і розрахуємось.", HeroPhrase), fx: "kill"));
}
