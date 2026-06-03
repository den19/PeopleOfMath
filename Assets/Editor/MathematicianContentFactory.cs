using System.Collections.Generic;
using PeopleOfMath.Data;
using UnityEditor;
using UnityEngine;

namespace PeopleOfMath.Editor
{
    public static class MathematicianContentFactory
    {
        public static List<MathematicianData> CreateAll(string folder)
        {
            var list = new List<MathematicianData>
            {
                Create(folder, BuildPythagoras()),
                Create(folder, BuildEuclid()),
                Create(folder, BuildArchimedes()),
                Create(folder, BuildNewton()),
                Create(folder, BuildEuler()),
                Create(folder, BuildGauss()),
                Create(folder, BuildLobachevsky()),
                Create(folder, BuildKovalevskaya()),
                Create(folder, BuildPoincare()),
                Create(folder, BuildTuring()),
            };
            return list;
        }

        static MathematicianData Create(string folder, MathematicianData template)
        {
            var path = $"{folder}/{template.id}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<MathematicianData>(path);
            if (existing != null)
            {
                CopyFields(template, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var asset = ScriptableObject.CreateInstance<MathematicianData>();
            CopyFields(template, asset);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static MathematicianData CreateTemplate() =>
            ScriptableObject.CreateInstance<MathematicianData>();

        static void CopyFields(MathematicianData from, MathematicianData to)
        {
            to.id = from.id;
            to.fullNameRu = from.fullNameRu;
            to.fullNameEn = from.fullNameEn;
            to.birthDate = from.birthDate;
            to.deathDate = from.deathDate;
            to.countryKeys = new List<string>(from.countryKeys);
            to.centuryKeys = new List<string>(from.centuryKeys);
            to.branchKeys = new List<string>(from.branchKeys);
            to.achievementsRu = from.achievementsRu;
            to.achievementsEn = from.achievementsEn;
            to.personalLifeRu = from.personalLifeRu;
            to.personalLifeEn = from.personalLifeEn;
            to.shortBioRu = from.shortBioRu;
            to.shortBioEn = from.shortBioEn;
            to.wikipediaUrlRu = from.wikipediaUrlRu;
        }

        static MathematicianData BuildPythagoras()
        {
            var m = CreateTemplate();
            m.id = "pythagoras";
            m.fullNameRu = "Пифагор";
            m.fullNameEn = "Pythagoras";
            m.birthDate = "ок. 570 до н.э.";
            m.deathDate = "ок. 495 до н.э.";
            m.countryKeys = new List<string> { "greece" };
            m.centuryKeys = new List<string> { "6bc", "5bc" };
            m.branchKeys = new List<string> { "geometry", "number_theory" };
            m.shortBioRu = "Древнегреческий философ и математик, основатель пифагорейской школы.";
            m.shortBioEn = "Ancient Greek philosopher and mathematician, founder of the Pythagorean school.";
            m.achievementsRu =
                "Сформулировал знаменитую теорему о соотношении сторон прямоугольного треугольника. " +
                "Заложил основы теории чисел и музыкальной гармонии через числовые отношения. " +
                "Пифагорейцы развивали идею рационального описания природы.";
            m.achievementsEn =
                "Formulated the famous theorem on the ratios of sides in a right triangle. " +
                "Laid foundations of number theory and musical harmony through numerical ratios. " +
                "Pythagoreans advanced the idea of a rational description of nature.";
            m.personalLifeRu =
                "Родился на острове Самос; позже основал общину последователей в Кротоне. " +
                "Жизнь окружена легендами: ученики соблюдали строгие правила, включая молчание. " +
                "Точные биографические детали частично переданы поздними авторами.";
            m.personalLifeEn =
                "Born on the island of Samos; later founded a community of followers in Croton. " +
                "His life is surrounded by legends: pupils followed strict rules, including periods of silence. " +
                "Precise biographical details were partly transmitted by later authors.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Пифагор";
            return m;
        }

        static MathematicianData BuildEuclid()
        {
            var m = CreateTemplate();
            m.id = "euclid";
            m.fullNameRu = "Евклид";
            m.fullNameEn = "Euclid";
            m.birthDate = "ок. 325 до н.э.";
            m.deathDate = "ок. 265 до н.э.";
            m.countryKeys = new List<string> { "greece" };
            m.centuryKeys = new List<string> { "3bc" };
            m.branchKeys = new List<string> { "geometry" };
            m.shortBioRu = "Автор «Начал» — фундаментального труда по геометрии античности.";
            m.shortBioEn = "Author of the Elements, the fundamental treatise on ancient geometry.";
            m.achievementsRu =
                "Систематизировал геометрию в 13 книгах «Начал», введя аксиоматический метод. " +
                "Алгоритм нахождения наибольшего общего делителя известен как алгоритм Евклида. " +
                "Его подход определял преподавание математики более двух тысячелетий.";
            m.achievementsEn =
                "Systematized geometry in thirteen books of the Elements, introducing the axiomatic method. " +
                "The algorithm for the greatest common divisor is known as Euclid's algorithm. " +
                "His approach shaped mathematics education for more than two millennia.";
            m.personalLifeRu =
                "Преподавал в Александрии при Птолемеях; сведений о личной жизни сохранилось мало. " +
                "Античные источники называют его «отцом геометрии». " +
                "Достоверные портреты и подробная биография неизвестны.";
            m.personalLifeEn =
                "Taught in Alexandria under the Ptolemies; little is known about his personal life. " +
                "Ancient sources call him the father of geometry. " +
                "Reliable portraits and a detailed biography are unknown.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Евклид";
            return m;
        }

        static MathematicianData BuildArchimedes()
        {
            var m = CreateTemplate();
            m.id = "archimedes";
            m.fullNameRu = "Архимед";
            m.fullNameEn = "Archimedes";
            m.birthDate = "ок. 287 до н.э.";
            m.deathDate = "212 до н.э.";
            m.countryKeys = new List<string> { "syracuse" };
            m.centuryKeys = new List<string> { "3bc" };
            m.branchKeys = new List<string> { "geometry", "mechanics" };
            m.shortBioRu = "Крупнейший учёный эллинистической эпохи, родом из Сиракуз.";
            m.shortBioEn = "Leading scientist of the Hellenistic era, from Syracuse.";
            m.achievementsRu =
                "Открыл закон гидростатики и развил методы вычисления площадей и объёмов. " +
                "Создал инженерные механизмы для защиты Сиракуз, изучал рычаги и винт. " +
                "Ввёл понятие π и оценил его с высокой точностью.";
            m.achievementsEn =
                "Discovered the law of hydrostatics and advanced methods for areas and volumes. " +
                "Designed engineering devices to defend Syracuse and studied levers and the screw. " +
                "Introduced the concept of π and estimated it with high accuracy.";
            m.personalLifeRu =
                "Сын астронома Фидия; долгое время жил в Александрии, затем вернулся на родину. " +
                "Погиб во время осады Сиракуз римлянами. " +
                "Известна история о крике «Эврика!» при исследовании короны.";
            m.personalLifeEn =
                "Son of the astronomer Phidias; lived in Alexandria for a long time, then returned home. " +
                "Died during the Roman siege of Syracuse. " +
                "The story of shouting Eureka while studying the king's crown is well known.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Архимед";
            return m;
        }

        static MathematicianData BuildNewton()
        {
            var m = CreateTemplate();
            m.id = "newton";
            m.fullNameRu = "Исаак Ньютон";
            m.fullNameEn = "Isaac Newton";
            m.birthDate = "4 января 1643";
            m.deathDate = "31 марта 1727";
            m.countryKeys = new List<string> { "england" };
            m.centuryKeys = new List<string> { "17" };
            m.branchKeys = new List<string> { "analysis", "mechanics" };
            m.shortBioRu = "Создатель классической механики и соавтор математического анализа.";
            m.shortBioEn = "Creator of classical mechanics and co-inventor of calculus.";
            m.achievementsRu =
                "Сформулировал три закона движения и закон всемирного тяготения. " +
                "Развил методы бесконечно малых (исчисление потоков). " +
                "«Математические начала натуральной философии» стали образцом научного метода.";
            m.achievementsEn =
                "Formulated the three laws of motion and the law of universal gravitation. " +
                "Developed methods of infinitesimals (fluxions). " +
                "The Principia became a model of scientific method.";
            m.personalLifeRu =
                "Родился в Вулсторпе; после смерти отца воспитывался матерью и бабушкой. " +
                "Работал в Кембридже и Лондоне, был президентом Лондонского королевского общества. " +
                "Известен периодами уединённой работы и острыми научными спорами.";
            m.personalLifeEn =
                "Born in Woolsthorpe; after his father's death he was raised by his mother and grandmother. " +
                "Worked in Cambridge and London, presided over the Royal Society. " +
                "Known for periods of solitary work and sharp scientific disputes.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Ньютон,_Исаак";
            return m;
        }

        static MathematicianData BuildEuler()
        {
            var m = CreateTemplate();
            m.id = "euler";
            m.fullNameRu = "Леонард Эйлер";
            m.fullNameEn = "Leonhard Euler";
            m.birthDate = "15 апреля 1707";
            m.deathDate = "18 сентября 1783";
            m.countryKeys = new List<string> { "switzerland", "russia" };
            m.centuryKeys = new List<string> { "18" };
            m.branchKeys = new List<string> { "analysis", "number_theory", "geometry" };
            m.shortBioRu = "Один из самых плодовитых математиков в истории.";
            m.shortBioEn = "One of the most prolific mathematicians in history.";
            m.achievementsRu =
                "Внёс вклад в анализ, теорию чисел, графы и механику. " +
                "Ввёл обозначения e, i, f(x) и многие другие. " +
                "Решил знаменитую задачу о кёнигсбергских мостах.";
            m.achievementsEn =
                "Contributed to analysis, number theory, graphs, and mechanics. " +
                "Introduced notation such as e, i, and f(x). " +
                "Solved the famous Königsberg bridges problem.";
            m.personalLifeRu =
                "Родился в Базеле; работал в Петербургской и Берлинской академиях. " +
                "После потери зрения продолжал вычисления и публикации. " +
                "Был женат дважды, имел тринадцать детей.";
            m.personalLifeEn =
                "Born in Basel; worked at the academies of Saint Petersburg and Berlin. " +
                "Continued calculations and publications after losing his sight. " +
                "Married twice and had thirteen children.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Эйлер,_Леонард";
            return m;
        }

        static MathematicianData BuildGauss()
        {
            var m = CreateTemplate();
            m.id = "gauss";
            m.fullNameRu = "Карл Фридрих Гаусс";
            m.fullNameEn = "Carl Friedrich Gauss";
            m.birthDate = "30 апреля 1777";
            m.deathDate = "23 февраля 1855";
            m.countryKeys = new List<string> { "germany" };
            m.centuryKeys = new List<string> { "18", "19" };
            m.branchKeys = new List<string> { "number_theory", "geometry" };
            m.shortBioRu = "«Princeps mathematicorum» — принц математиков.";
            m.shortBioEn = "The Prince of Mathematicians.";
            m.achievementsRu =
                "Доказал теорему о распределении простых чисел в арифметических прогрессиях. " +
                "Развил теорию ошибок наблюдений и геодезию. " +
                "Создал внутреннюю геометрию поверхностей.";
            m.achievementsEn =
                "Proved the theorem on primes in arithmetic progressions. " +
                "Developed the theory of observational errors and geodesy. " +
                "Created the intrinsic geometry of surfaces.";
            m.personalLifeRu =
                "Родился в Брауншвейге в семье простых людей; проявил выдающийся талант в детстве. " +
                "Работал в Гёттингене, редко путешествовал. " +
                "После смерти первой жены долгое время переживал утрату.";
            m.personalLifeEn =
                "Born in Brunswick into a modest family; showed exceptional talent as a child. " +
                "Worked in Göttingen and rarely traveled. " +
                "Grieved deeply after the death of his first wife.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Гаусс,_Карл_Фридрих";
            return m;
        }

        static MathematicianData BuildLobachevsky()
        {
            var m = CreateTemplate();
            m.id = "lobachevsky";
            m.fullNameRu = "Николай Иванович Лобачевский";
            m.fullNameEn = "Nikolai Lobachevsky";
            m.birthDate = "1 декабря 1792";
            m.deathDate = "24 февраля 1856";
            m.countryKeys = new List<string> { "russia" };
            m.centuryKeys = new List<string> { "19" };
            m.branchKeys = new List<string> { "geometry" };
            m.shortBioRu = "Создатель неевклидовой геометрии на плоскости.";
            m.shortBioEn = "Creator of hyperbolic non-Euclidean geometry.";
            m.achievementsRu =
                "Построил геометрию Лобачевского, отказавшись от пятого постулата Евклида. " +
                "Реформировал преподавание математики в Казанском университете. " +
                "Работал над аналитическими методами и приближениями.";
            m.achievementsEn =
                "Built Lobachevskian geometry by rejecting Euclid's fifth postulate. " +
                "Reformed mathematics teaching at Kazan University. " +
                "Worked on analytic methods and approximations.";
            m.personalLifeRu =
                "Родился в Нижнем Новгороде; рано лишился отца. " +
                "Был ректором Казанского университета много лет, женился дважды, имел много детей. " +
                "В конце жизни ослеп, но продолжал диктовать работы.";
            m.personalLifeEn =
                "Born in Nizhny Novgorod; lost his father early. " +
                "Served as rector of Kazan University for many years, married twice, had many children. " +
                "Became blind late in life but continued dictating his work.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Лобачевский,_Николай_Иванович";
            return m;
        }

        static MathematicianData BuildKovalevskaya()
        {
            var m = CreateTemplate();
            m.id = "kovalevskaya";
            m.fullNameRu = "Софья Васильевна Ковалевская";
            m.fullNameEn = "Sofia Kovalevskaya";
            m.birthDate = "15 января 1850";
            m.deathDate = "10 февраля 1891";
            m.countryKeys = new List<string> { "russia" };
            m.centuryKeys = new List<string> { "19" };
            m.branchKeys = new List<string> { "analysis" };
            m.shortBioRu = "Первая в России женщина — профессор математики.";
            m.shortBioEn = "The first woman in Russia to become a professor of mathematics.";
            m.achievementsRu =
                "Решила задачу о вращении твёрдого тела вокруг неподвижной точки. " +
                "Внесла вклад в теорию дифференциальных уравнений и абелевых интегралов. " +
                "Получила докторскую степень в Гёттингене.";
            m.achievementsEn =
                "Solved the problem of rotation of a rigid body about a fixed point. " +
                "Contributed to differential equations and Abelian integrals. " +
                "Earned a doctorate in Göttingen.";
            m.personalLifeRu =
                "Ради обучения за границей заключила фиктивный брак с В. О. Ковалевским. " +
                "Жила в России, Швеции и Франции; дружила с Анной Лебединской. " +
                "Погибла в Дании от эпидемии гриппа в возрасте 41 года.";
            m.personalLifeEn =
                "Entered a fictitious marriage with V. Kovalevsky to study abroad. " +
                "Lived in Russia, Sweden, and France; was close to Anna Lebedinskaya. " +
                "Died in Denmark from influenza at age 41.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Ковалевская,_Софья_Васильевна";
            return m;
        }

        static MathematicianData BuildPoincare()
        {
            var m = CreateTemplate();
            m.id = "poincare";
            m.fullNameRu = "Анри Пуанкаре";
            m.fullNameEn = "Henri Poincaré";
            m.birthDate = "29 апреля 1854";
            m.deathDate = "17 июля 1912";
            m.countryKeys = new List<string> { "france" };
            m.centuryKeys = new List<string> { "19", "20" };
            m.branchKeys = new List<string> { "topology", "dynamical_systems" };
            m.shortBioRu = "Универсальный математик и теоретик науки fin de siècle.";
            m.shortBioEn = "A universal mathematician and philosopher of science at the fin de siècle.";
            m.achievementsRu =
                "Заложил основы топологии и качественной теории дифференциальных уравнений. " +
                "Сформулировал гипотезу Пуанкаре (доказана Перельманом). " +
                "Внёс вклад в теорию относительности и небесной механики.";
            m.achievementsEn =
                "Founded topology and qualitative theory of differential equations. " +
                "Formulated the Poincaré conjecture (proved by Perelman). " +
                "Contributed to relativity and celestial mechanics.";
            m.personalLifeRu =
                "Родился в Нанси; учился в Политехнической школе и Нормальной школе. " +
                "Был членом Французской академии наук, активно популяризировал науку. " +
                "Женился на Жюстин Боннэ, имел четверых детей.";
            m.personalLifeEn =
                "Born in Nancy; studied at the Polytechnic and Normal schools. " +
                "Was a member of the French Academy of Sciences and popularized science. " +
                "Married Justine Bonne and had four children.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Пуанкаре,_Анри";
            return m;
        }

        static MathematicianData BuildTuring()
        {
            var m = CreateTemplate();
            m.id = "turing";
            m.fullNameRu = "Алан Тьюринг";
            m.fullNameEn = "Alan Turing";
            m.birthDate = "23 июня 1912";
            m.deathDate = "7 июня 1954";
            m.countryKeys = new List<string> { "uk" };
            m.centuryKeys = new List<string> { "20" };
            m.branchKeys = new List<string> { "logic", "informatics" };
            m.shortBioRu = "Основоположник информатики и теоретической логики.";
            m.shortBioEn = "Founder of computer science and theoretical logic.";
            m.achievementsRu =
                "Предложил абстрактную машину Тьюринга — модель вычислений. " +
                "Участвовал в дешифровке «Энигмы» на Блетчли-парке. " +
                "Разработал тест Тьюринга для оценки машинного интеллекта.";
            m.achievementsEn =
                "Introduced the Turing machine as a model of computation. " +
                "Contributed to breaking the Enigma cipher at Bletchley Park. " +
                "Proposed the Turing test for machine intelligence.";
            m.personalLifeRu =
                "Родился в Лондоне; учился в Кембридже и Принстоне. " +
                "Во время войны работал на секретных проектах; после войны занимался вычислительными машинами. " +
                "Трагически рано скончался; в 2013 году получил посмертное помилование.";
            m.personalLifeEn =
                "Born in London; studied at Cambridge and Princeton. " +
                "Worked on secret wartime projects; later built early computers. " +
                "Died young; received a posthumous royal pardon in 2013.";
            m.wikipediaUrlRu = "https://ru.wikipedia.org/wiki/Тьюринг,_Алан";
            return m;
        }
    }
}
