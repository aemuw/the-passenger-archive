using Godot;

namespace PassengerArchive;

/// <summary>
/// Hand-authored content. Every carriage is built differently on purpose (see docs/DESIGN.md):
///   1 — one talkable person + a played memory + a hidden rule (fiction)
///   2 — two people talking to each other: you overhear, may interrupt, and one of them lets you see her imagined memory (inspired)
///   3 — a social carriage: victim, witness, newspaper reader, liar, friendly killer (inspired)
///   4 — the hero's own carriage and memories (hero)
/// The hero's own killer (the "Попутник") is in carriages 1–3 in different clothes, always with the same tells.
///
/// Dialogue: nodes with options. What the hero picks changes what people say (flags), and some choices
/// are remembered for the whole run ("k:…") — the man who rides with you quotes them at the very end.
/// </summary>
public static class Carriages
{
	public const string HeroPhrase = "Далі — то далі.";

	// Looks that must stay recognisable between carriage and memory.
	public static readonly Outfit KepkaLook = new(new Color(0.30f, 0.30f, 0.32f), Hat: true, HatColor: new Color(0.16f, 0.22f, 0.17f));
	public static readonly Outfit PopOriginal = new(new Color(0.14f, 0.14f, 0.16f), Hat: true, HatColor: new Color(0.09f, 0.09f, 0.10f), LongCoat: true, Collar: true);
	public static readonly Outfit TarasLook = new(new Color(0.55f, 0.55f, 0.52f));
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
	static PersonData BG(string id, int row, int side, int slot, Outfit look) => new(id, null, null, row, side, slot, look);

	static readonly Vector3 ConductorSpot = new(-1.15f, 0f, -6.45f);

	// ───────────── the conductor: never explains, never lies ─────────────
	static readonly Convo ConductorTalk = C(
		new[] { E("hi"), E("back", need: "c_met") },
		N("hi", S("Слухаю."), next: "hub", set: "c_met"),
		N("back", S("Так?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Куди їде потяг?", "where", unless: "c_where"),
			O("Хто ці люди?", "who", unless: "c_who"),
			O("Чому вони тут?", "why", unless: "c_why"),
			O("Що в наступному вагоні?", "next", unless: "c_next"),
			O("Нічого. Дякую."),
		}),
		N("where", S("Далі."), next: "hub", set: "c_where"),
		N("who", S("Пасажири."), next: "hub", set: "c_who"),
		N("why", S("А чому ти?"), next: "hub", set: "c_why"),
		N("next", S("Знаю."), set: "c_next", opts: new[] { O("То скажіть.", "next2"), O("Ясно.", "hub") }),
		N("next2", S("Це не моя історія."), next: "hub"));

	static PersonData Conductor(Vector3 at, float yaw, Convo talk = null) => new(
		Id: "conductor", Name: "Провідник", Alias: "Провідник", Row: 0, Side: 0, Slot: 0,
		Look: new Outfit(new Color(0.12f, 0.2f, 0.18f), Hat: true, HatColor: new Color(0.1f, 0.16f, 0.14f), Collar: true),
		Dialog: talk ?? ConductorTalk, StandAt: at, StandYaw: yaw);

	// ═════════════════════════════ CARRIAGE 1 · Viktor ═════════════════════════════
	static readonly Convo ViktorTalk = C(
		new[] { E("first"), E("again", need: "v_met"), E("done", need: "story_done") },

		N("first", S("Ви теж чекаєте, поки скажуть, де виходити?"), opts: new[]
		{
			O("Тут вільно?", "free"),
			O("Ви в порядку?", "ok"),
			O("Куди їде цей потяг?", "where"),
		}),
		N("free", S("Вільно. Тут усе вільно. Я навіть не пам'ятаю, як сів — ні квитка, ні станції."), set: "v_met", next: "hub"),
		N("ok", S("Я? Так. Ні. Не знаю.", "Ви перший, хто питає. Решта більше мовчать."), set: "v_met,v_ok", next: "hub"),
		N("where", S("Питайте провідника. Він скаже «далі». Мені цього вистачило, щоб замовкнути."), set: "v_met", next: "hub"),

		N("again", S("Так?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Як вас звати?", "name", unless: "v_name", set: "v_name"),
			O("Де ви живете?", "home", unless: "v_home"),
			O("Хто ці люди навколо?", "people", unless: "v_people"),
			O("Розкажіть, як ви добиралися додому.", "route", need: "v_home"),
			O("Не буду заважати."),
		}),
		N("name", S("Віктор. Давно ніхто не питав."), fx: "know", next: "hub"),
		N("home", S("Недалеко від роботи. П'ятиповерхівка, третій під'їзд. Двадцять років ходив одним шляхом: зупинка, ліхтарі, арка, під'їзд.", "Із заплющеними очима пройшов би."), set: "v_home", next: "hub"),
		N("people", S("Он дівчина в шарфі — спить. Он той, у синьому, теж.", "А чоловік у кепці… сидить, ніби нічого не робить, а сам усе барабанить пальцями. Ніби рахує."),
			set: "v_people", opts: new[] { O("Ви його знаєте?", "kepka"), O("Зрозуміло.", "hub") }),
		N("kepka", S("Не знаю. А здається, що знаю.", "Так буває, коли людину бачиш щодня, але ніколи не дивишся їй в обличчя."), next: "hub"),
		N("route", S("Та що там розповідати. Ті самі ліхтарі, та сама арка.", "Покажу, якщо хочете. Тільки не втручайтесь. Просто йдіть позаду."),
			opts: new[] { O("Покажіть.", "show"), O("Може, пізніше.", "hub") }),
		N("show", S("Дивіться."), fx: "memory:route"),

		N("done", S("Тепер ви знаєте, як це було.", "Я пробував пригадати, де саме… Нічого не змінити."), opts: new[]
		{
			O("Мені шкода.", "sorry"),
			O("А чоловік у кепці?", "kepka2"),
			O("Їдьте спокійно."),
		}),
		N("sorry", S("Не треба. Просто їдьте далі."), next: null),
		N("kepka2", S("Не питайте мене про нього. Я його вже не бачу.", "Він тільки сидить. Ніби чекає, поки хтось візьме слухавку."), next: null));

	static readonly Convo KepkaTalk = C(
		new[] { E("first"), E("seen", need: "mem_route") },
		N("first", S("Що?"), opts: new[]
		{
			O("Нічого. Просто питаю, куди ви їдете.", "go"),
			O("Ви часто так їздите?", "often"),
			O("Вибачте."),
		}),
		N("go", S("Туди ж, куди й усі."), next: null),
		N("often", S("Не знаю. А ви?"), opts: new[] { O("Не пам'ятаю.", "mem"), O("Вперше.", "first_time") }),
		N("mem", S("Це добре. Хто не пам'ятає — спокійно живе."), next: null),
		N("first_time", S("Вперше. Я теж колись був вперше."), next: null),
		N("seen", S("Ви мене десь бачили?"), opts: new[]
		{
			O("На зупинці. Біля арки.", "stop"),
			O("Ні. Вам здалося.", "no"),
		}),
		N("stop", S("Багато де стоять люди.", "Я просто їду."), next: null),
		N("no", S("Тоді не дивіться так довго."), next: null));

	static readonly Convo Pop1Talk = C(
		new[] { E("first") },
		N("first", S("Не заважайте. Я думаю."), opts: new[]
		{
			O("Вибачте."),
			O("Про що?", "what"),
		}),
		N("what", S("Про дорогу.", HeroPhrase), set: "k:met_pop1"));

	// ═════════════════════════════ CARRIAGE 2 · Halyna & Olya ═════════════════════════════
	static readonly Convo GalynaTalk = C(
		new[] { E("first"), E("heard", need: "heard"), E("interrupt", need: "overheard_on", unless: "heard"), E("done", need: "story_done") },

		N("first", S("Не треба. Дякую, що підійшли.", "Ви його не знали. І добре."), next: null),
		N("interrupt", S("Не зараз, будь ласка."), opts: new[]
		{
			O("Вибачте, я випадково почув.", "sorry"),
			O("Я піду."),
		}),
		N("sorry", S("Нічого. Ви не перший, хто слухає.", "Тут усі слухають. А потім їдуть далі."), next: null),

		N("heard", S("А, ви все чули. Нічого, не вибачайтесь."), opts: new[]
		{
			O("Це був ваш чоловік?", "husband", unless: "g_h"),
			O("Ви були там після?", "wind", unless: "g_w"),
			O("Розкажіть, як усе було.", "tell", need: "g_h"),
			O("Не буду нав'язуватись."),
		}),
		N("husband", S("Андрій. П'ятнадцять років.", "Він усіх підвозив: сусідів, студентів, діда з вокзалу. Казав: «Люди ж»."), set: "g_h", fx: "know", next: "heard"),
		N("wind", S("Один раз. Там вітер. Квіти кладуть — вітер їх розносить.", "Я постояла й поїхала. Що там стояти?"), set: "g_w", next: "heard"),
		N("tell", S("Я не бачила. Але я так багато разів це уявляла, що, здається, була там.",
				"Хочете — дивіться. Тільки не питайте, який він був з обличчя. Я його не бачила."),
			opts: new[] { O("Покажіть.", "show"), O("Може, не варто.", "heard") }),
		N("show", S("Тоді просто йдіть по дорозі."), fx: "memory:ride"),

		N("done", S("Він був хорошою людиною. Це найгірше."), opts: new[]
		{
			O("Що б ви йому сказали тоді?", "say"),
			O("Мені шкода."),
		}),
		N("say", S("Що мені страшно. Просто це. Одне речення.", "А я боялась, що він розсміється."), next: null));

	static readonly Convo OlyaTalk = C(
		new[] { E("first"), E("done", need: "story_done") },
		N("first", S("Я не знаю, що сказати. Тут нічого не скажеш."), opts: new[]
		{
			O("Це ваші квіти?", "flowers"),
			O("Я не хотів завадити."),
		}),
		N("flowers", S("Гвоздики. Він любив.", "Галя думає, що я їх для нього. А я — щоб було що тримати в руках."), opts: new[]
		{
			O("Ви були близькі?", "close"),
			O("Зрозуміло."),
		}),
		N("close", S("Ми десять років сиділи в одному кабінеті. Він мене щодня підвозив.", "Тієї ночі я була у відпустці. Я теж думаю: якби я була в машині…"), next: null),
		N("done", S("Я хотіла, щоб квіти були від нас обох."), next: null));

	static readonly Convo Pop2Talk = C(
		new[] { E("first"), E("again", need: "k:met_pop2") },
		N("first", S("О, жива душа. Сідайте, місця вистачає.", "Довго їдете?"), set: "k:met_pop2", opts: new[]
		{
			O("Не пам'ятаю, звідки.", "mem", set: "k:said_dont_remember"),
			O("До кінцевої.", "end", set: "k:said_terminus"),
			O("А ви?", "you"),
		}),
		N("mem", S("Я теж. Забавно, правда? Сідаєш — і вже їдеш.", "Мені подобається, коли є з ким поговорити."), next: "hub"),
		N("end", S("До кінцевої! Далеко замахнулись.", "Люблю тих, хто знає, куди їде. Мені б так."), next: "hub"),
		N("you", S("Я? Я просто їду. Поруч.", "Люблю, коли є з ким."), next: "hub"),
		N("again", S("О, це ви. Ну що, влаштувались?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Що ви читаєте?", "paper", unless: "p2_paper"),
			O("Ви бачили, що тут було з тими двома?", "women", unless: "p2_women"),
			O("Як вас звати?", "name", unless: "p2_name"),
			O("Дякую, мені пора."),
		}),
		N("paper", S("Читаю? Так, звісно. Нічого цікавого. Одні й ті ж новини.", "Хтось пішов не туди, хтось не туди повернув."), set: "p2_paper", next: "hub"),
		N("women", S("Бачив. Горе таке, що й сказати нічого. Я сів подалі."), set: "p2_women", next: "hub"),
		N("name", S("Звіть як хочете. Мене всі по-різному звуть."), set: "p2_name,k:asked_pop_name", next: "hub"),

		// Played when the player tries the stuck door (Game starts this node by itself).
		N("door", S("Заклинило? Дайте я.", "Ви вже не вперше з такими дверима?"), opts: new[]
		{
			O("Дякую.", "door_thanks", set: "k:accepted_help"),
			O("Я сам.", "door_self", set: "k:refused_help"),
		}),
		N("door_thanks", S("Та нічого. Усі колись потребують, щоб хтось відчинив.", HeroPhrase), fx: "unstick"),
		N("door_self", S("Як хочете. Вона так не піддасться, але нехай.", "Ну от. Піддалась. Буває.", HeroPhrase), fx: "unstick"));

	// ═════════════════════════════ CARRIAGE 3 · five voices ═════════════════════════════
	static readonly Convo SolomiyaTalk = C(
		new[] { E("first"), E("again", need: "s_met"), E("done", need: "story_done") },

		N("first", S("Ви теж їдете додому?"), opts: new[]
		{
			O("Я не знаю, куди їду.", "dunno"),
			O("Так, додому.", "home"),
			O("Ви в порядку?", "ok"),
		}),
		N("dunno", S("Тоді ми схожі. Тільки я знаю, куди їхала. Просто не доїхала."), set: "s_met", next: "hub"),
		N("home", S("Це добре. Додому — це добре. Я теж їхала додому."), set: "s_met", next: "hub"),
		N("ok", S("Бувало краще. Я просто хотіла дістатися додому. Всього лише."), set: "s_met", next: "hub"),

		N("again", S("Так?"), next: "hub"),
		N("hub", None, opts: new[]
		{
			O("Як вас звати?", "name", unless: "s_name", set: "s_name"),
			O("Що сталося?", "what"),
			O("Не буду заважати."),
		}),
		N("name", S("Соломія."), fx: "know", next: "hub"),
		N("what", S("П'ятниця. Пізно. Автобус не прийшов.", "Компанія розходилась. Я не хотіла здіймати галас."), opts: new[]
		{
			O("Треба було викликати таксі.", "taxi"),
			O("Що було далі?", "next"),
		}),
		N("taxi", S("Гроші були на рахунки. Тоді здавалося — зайве.", "Знаєте, як воно: поки не трапилось, усе здається зайвим."), next: "next"),
		N("next", S("Один сказав: «Темно. Давайте я вас проведу».", "Я погодилась. Мені здалось, це ввічливо.", "Я не можу вам просто розповісти це. Хочете побачити?"),
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

	static readonly Convo HannaTalk = C(
		new[] { E("first") },
		N("first", S("Ви читали? Знову на пустирі за вокзалом."), opts: new[]
		{
			O("Що там?", "what"),
			O("Я не читаю новин."),
		}),
		N("what", S("Дівчину знайшли. Пишуть — грабіжник, ззаду підійшов.", "Господи, а ми цими потягами щодня."), opts: new[]
		{
			O("Грабіжник? Так вирішили?", "police"),
			O("Вона була не сама?", "alone"),
			O("Страшно.", "scary"),
		}),
		N("police", S("Ну, так пишуть. Я ж не слідчий.", "Хоча дивно: сумку не взяли."), next: "phrase"),
		N("alone", S("Не сама? Звідки ви знаєте? У газеті про це ні слова.", "Хоча… так, це було б логічно."), next: "phrase"),
		N("scary", S("Ще й як."), next: "phrase"),
		N("phrase", S("Мені один чоловік сказав: «Далі — то далі». Так спокійно, я аж…"), opts: new[]
		{
			O("Який чоловік?", "man"),
			O("Мало що люди кажуть."),
		}),
		N("man", S("У коричневому пальті. Біля дверей стояв. Спокійний такий.", "Усе одно я не люблю, коли так спокійно."), set: "k:hanna_pop", next: null));

	static readonly Convo BohdanTalk = C(
		new[] { E("first") },
		N("first", S("Я бачив її з чоловіком. Привітний такий, у світлому светрі."), opts: new[]
		{
			O("Чому ви не втрутились?", "why"),
			O("Ви розгледіли його?", "face"),
		}),
		N("why", S("Що мені було робити? Втручатись? Я нічого не міг зробити.", "Сказати: «Ви її знаєте?» А якщо знає?"), opts: new[]
		{
			O("Могли хоча б подзвонити.", "call"),
			O("Я розумію.", "understand"),
		}),
		N("call", S("Хтось би подзвонив. Усі так думають.", "«Хтось подзвонить»."), next: null),
		N("understand", S("Ні, не розумієте. Я теж не розумію."), next: null),
		N("face", S("Світло там було погане. Нічого не розгледів.", "Лише светр. Світлий. Дорогий, певно."), next: null));

	static readonly Convo LevkoTalk = C(
		new[] { E("first") },
		N("first", S("Нічого я не бачив. Вона сама пішла. Я того вечора вдома був."), opts: new[]
		{
			O("Я не питав про вечір.", "slip"),
			O("Хто тоді був?", "who"),
		}),
		N("slip", S("Ну… я просто. Мало що люди плетуть.", "Чого ви так дивитесь? Вдома, кажу."), set: "l_slip", opts: new[] { O("Хто тоді був?", "who"), O("Гаразд.") }),
		N("who", S("Спитайте в нього. Він усе знає."), set: "l_pointed", opts: new[]
		{
			O("У кого?", "taras"),
			O("Не буду вас більше мучити."),
		}),
		N("taras", S("У нашого тямущого. Світлий светр. Він завжди знає, хто куди пішов."), next: null));

	static readonly Convo TarasTalk = C(
		new[] { E("first"), E("after", need: "offer_declined"), E("confess", need: "t_asked_story") },

		N("first", S("Добрий вечір. Місця тут вистачає."), opts: new[]
		{
			O("Ви тут давно?", "long"),
			O("Ви не знали дівчину в зеленому пальті?", "know"),
			O("Левко каже, що ви все знаєте.", "levko", need: "l_pointed"),
			O("Дякую."),
		}),
		N("long", S("Від початку. Люблю потяги. Тут усі розповідають.", "Не слухайте."), next: null),
		N("know", S("Ні. Хто ж їх знає? Я з жінками обережний.", "У мене сестра така сама — довірлива."), next: null),
		N("levko", S("Левко? Вигадує. Він завжди вигадує.", "Він би і рідного батька виправдав. Або звинуватив."), next: null),

		N("after", S("Я завжди хотів допомогти. Такі, як вона, ходять самі, довірливі."), opts: new[]
		{
			O("Розкажіть, як усе було.", "story"),
			O("Ви її вбили.", "accuse"),
			O("Я піду."),
		}),
		N("accuse", S("Я? Ні. Я просто розповідаю історію. Тут усі розповідають."), next: "after"),
		N("story", S("Я чекав на зупинці. Просто стояв, нічого такого.", "Вона виходила з компанії сама. Я сказав: «Темно, давайте проведу». Вона посміхнулась.",
				"Ввічлива. Такі завжди ввічливі.", "Біля арки сказала «ні». Тихо. А я вже не міг повернути назад."),
			set: "t_asked_story", next: "story2"),
		N("story2", S("Не думайте, що я чудовисько. Я звичайна людина.", "У цьому й вся проблема."), opts: new[]
		{
			O("Чому ви мені це кажете?", "why"),
			O("Мовчати."),
		}),
		N("why", S("Бо ви не пішли зі мною. Поки що все добре."), next: null),
		N("confess", S("Ви хочете ще раз послухати? Я не проти."), next: "story"),

		// Started by the game when the lights die, 35 s after you enter.
		N("offer", S("Темно. Давайте я вас проведу до дверей."), opts: new[]
		{
			O("Дякую, ходімо.", "go"),
			O("Ні, я сам.", "no"),
			O("(Промовчати.)", "silent"),
		}),
		N("go", S("Ходімо."), fx: "violate"),
		N("no", S("Як знаєте. Тоді сидіть тихо."), set: "offer_declined"),
		N("silent", S("Мовчите? Ну, не заважайте."), set: "offer_declined"));

	static readonly Convo Pop3Talk = C(
		new[] { E("first") },
		N("first", S("Ви теж дивитесь на двері? Звичка."), opts: new[]
		{
			O("Ви читали газету в сусідньому вагоні.", "lie", need: "k:met_pop2"),
			O("Ви тут давно?", "long"),
			O("Дякую."),
		}),
		N("lie", S("Газету? Ні. Ви, мабуть, сплутали. Тут усі схожі.", HeroPhrase), set: "k:caught_pop_lie"),
		N("long", S("Я їду. Як і ви."), next: null));

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
		N("pop", S("Пасажир."), set: "h_pop", next: "ask"),
		N("where", S("Туди, де ти вже був."), set: "h_where", next: "ask"),
		N("story", S("Розкажи свою історію."), fx: "memory:depart"),

		N("h2", S("Ти не вибирав цей потяг. Він вибрав момент."), next: "hub"),
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
					Row: 3, Side: -1, Slot: 1, Look: new Outfit(new Color(0.32f, 0.22f, 0.15f)), Dialog: ViktorTalk),

				// Killer of Viktor. Nothing about him is obvious — he just sits. Same cap, same tapping hand as in the memory.
				new PersonData("kepka", "Чоловік у кепці", "Незнайомець у зеленій кепці, що барабанить пальцями по коліну",
					Row: 5, Side: +1, Slot: 0, Look: KepkaLook, Dialog: KepkaTalk, Idle: Gesture.Tap, IsKiller: true),

				// The hero's own killer. Near the back door.
				new PersonData("pop", null, "Незнайомець у довгому пальті, що поправляє рукав",
					Row: 7, Side: -1, Slot: 0, Look: PopOriginal, Dialog: Pop1Talk, Idle: Gesture.Sleeve, IsHeroKiller: true),

				BG("bg1", 1, +1, 1, new Outfit(new Color(0.3f, 0.18f, 0.2f), Scarf: true, ScarfColor: new Color(0.55f, 0.5f, 0.3f))),
				BG("bg2", 2, -1, 0, new Outfit(new Color(0.2f, 0.28f, 0.4f))),
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
					Look: new Outfit(new Color(0.12f, 0.12f, 0.14f), Scarf: true, ScarfColor: new Color(0.35f, 0.35f, 0.38f)), Dialog: GalynaTalk),
				new PersonData("olya", "Оля", "Незнайомка з гвоздиками, яка не знає, куди їх діти",
					Row: 3, Side: +1, Slot: 1, Look: new Outfit(new Color(0.2f, 0.35f, 0.35f), Flowers: true), Dialog: OlyaTalk),

				// The hero's killer, now friendly: sits with a newspaper he never turns, helps with the stuck door.
				new PersonData("pop", null, "Незнайомець із газетою, що не перегортає сторінок",
					Row: 6, Side: -1, Slot: 0, Look: new Outfit(new Color(0.42f, 0.38f, 0.3f), Paper: true),
					Dialog: Pop2Talk, Idle: Gesture.Sleeve, IsHeroKiller: true),

				BG("bg1", 1, -1, 1, new Outfit(new Color(0.25f, 0.3f, 0.28f), Hat: true, HatColor: new Color(0.2f, 0.2f, 0.2f))),
				BG("bg2", 5, +1, 1, new Outfit(new Color(0.35f, 0.25f, 0.3f))),
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
					Look: new Outfit(new Color(0.22f, 0.36f, 0.3f), Scarf: true, ScarfColor: new Color(0.7f, 0.65f, 0.4f)), Dialog: SolomiyaTalk),

				new PersonData("hanna", "Ганна", "Незнайомка з газетою й виразом «ви не повірите»",
					Row: 4, Side: +1, Slot: 1, Look: new Outfit(new Color(0.35f, 0.3f, 0.22f), Paper: true), Dialog: HannaTalk),

				new PersonData("bohdan", "Богдан", "Незнайомець у сірому, що дивиться крізь стіну",
					Row: 5, Side: -1, Slot: 0, Look: new Outfit(new Color(0.25f, 0.25f, 0.3f)), Dialog: BohdanTalk),

				new PersonData("levko", "Левко", "Незнайомець у коричневому з піднятим коміром, що нікого не бачить",
					Row: 6, Side: +1, Slot: 0, Look: new Outfit(new Color(0.4f, 0.28f, 0.2f), Collar: true), Dialog: LevkoTalk),

				// Killer of Solomiya. Friendly. Offers to guide you when the lights go out.
				new PersonData("taras", "Тарас", "Незнайомець у світлому светрі, надто привітний для цього вагона",
					Row: 1, Side: -1, Slot: 0, Look: TarasLook, Dialog: TarasTalk, IsKiller: true),

				// The hero's killer, standing by the door.
				new PersonData("pop", null, "Незнайомець біля дверей, що ніби теж чекає зупинки",
					Row: 0, Side: 0, Slot: 0, Look: new Outfit(new Color(0.36f, 0.26f, 0.18f), Collar: true),
					Dialog: Pop3Talk, Idle: Gesture.Sleeve, StandAt: new Vector3(1.15f, 0f, -6.35f), StandYaw: 0f, IsHeroKiller: true),
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
		N("hi", S("Дякую, що зупинились. Годину тут стою, жоден не гальмує."), opts: new[]
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
		N("hi", S("Темно. Давайте я вас проведу."), opts: new[]
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

	/// <summary>Marta's call at the terminus. Same phone that ruined Viktor in carriage 1.</summary>
	public static readonly Convo MartaCall = C(
		new[] { E("hi") },
		N("hi", S("Нарешті! Ти де?", "Я тричі дзвонила."), opts: new[]
		{
			O("Я на кінцевій. Все гаразд.", "fine"),
			O("Пробач. Я просто хотів встигнути.", "sorry"),
			O("(Мовчати.)", "silent"),
		}),
		N("fine", S("Все гаразд… Ти ж не чекав на ранковий. Я просила."), next: "mid"),
		N("sorry", S("Знаю. Ти завжди поспішаєш."), next: "mid"),
		N("silent", S("Алло? Ти тут? Я чую, що ти дихаєш."), next: "mid"),
		N("mid", S("Не виходь зі станції, добре? Стій під ліхтарем.", "Я вже виходжу. Дві години — і я буду."), opts: new[]
		{
			O("Добре. Чекатиму.", "end", set: "k:promised_marta"),
			O("Я люблю тебе.", "love"),
		}),
		N("love", S("Я теж. Чуєш? Не відходь від ліхтаря."), next: "end2"),
		N("end", S("Обіцяєш?"), opts: new[] { O("Обіцяю.", "end2") }),
		N("end2", S("Тоді до зустрічі."), fx: "callend"));

	/// <summary>The man who has ridden with you since the first carriage. Every answer you gave him is remembered.</summary>
	public static readonly Convo KillerTalk = C(
		new[] { E("hi") },
		N("hi", S("Вибачте. Ви — з нічного, так?"), opts: new[]
		{
			O("Ми знайомі?", "met_a", need: "k:said_dont_remember"),
			O("Ми знайомі?", "met_b", need: "k:said_terminus", unless: "k:said_dont_remember"),
			O("Ми знайомі?", "met_c", need: "k:met_pop2", unless: "k:said_dont_remember,k:said_terminus"),
			O("Ми знайомі?", "unmet", unless: "k:met_pop2"),
			O("Я поспішаю.", "hurry"),
			O("Хто ви?", "who"),
		}),
		N("met_a", S("Звісно. Ми говорили в потязі. Ви сказали, що не пам'ятаєте, звідки їдете."), next: "end"),
		N("met_b", S("Звісно. Ви сказали, що їдете до кінцевої. Ось і кінцева."), next: "end"),
		N("met_c", S("Звісно. Ми говорили в потязі. Ви питали, куди їду я. Я сказав: поруч."), next: "end"),
		N("unmet", S("Ні. Але я вас запам'ятав. Ви їхали один."), next: "end"),
		N("hurry", S("Усі поспішають. А потім виявляється, що поспішати нікуди."), next: "end"),
		N("who", S("Ніхто. Пасажир. Як і ви."), next: "end"),
		N("end", S("Знаєте, мені подобаються люди, які мовчать, коли треба.", "Вам би почекати ранковий."), ifNeed: "k:heard_marta", ifTo: "heard", next: "last"),
		N("heard", S("Вам дзвонили тричі. Я чув.", "Вона просила почекати. Правда?"), next: "last"),
		N("last", S(HeroPhrase), fx: "kill"));
}
