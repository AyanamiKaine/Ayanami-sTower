# Possible Features

## [] Cloud Saves

I use the app on windows and linux it would be quite nice if we could painlessly sync them. Having a small server that does that is quite trival. Simply associate an ID with a filepath to the json save file, send that to the client and we are all good. To update it simply send the file to the server that then replaces it. We could even easily create a backup system.

Keep it simply and just create a key value store. Where the key is the USER_ID and the value the path to the json file. Read the file and send it to the client.

## [] Reading List

Sometimes my adhd brain cant read a book well because I am being distracted by other books. It would be nice if I could create a list of reading materials and creating an list of chapters I can marked as finished reading and being able to set some reminders. Something like reading chapters so and so is due.

## [] Content Queue

Videos, Blog posts, pictures, comments, content in general floods everything in my life. It would be desirable to have list of curated content that I can consume. Also it would be nice to have the distinguish between consumed content and to be consumed ones. This was inspired by a comment I read on hackernews (PS: I have to find that comment again).

## [] Theme Customization

It would be quite nice if the user can customize their theme, this would include acent colors, as well as some default color schemes. So for example you can have the Catppuccin color theme.

## [IMPLEMENTED] Spaced-Repetition

Anki is not the best learning tool because it ignores important aspects of learning. You cant easily create quizes or image occlusions. Also Anki does not nativly intergrate with existing learning apps like obsidian (We need a way to be able to write notes that are to be learned in obsidian and being able to open them with obsidian).

Anki does not have a priority queue. A priority queue is highly important because we dont want decks with specific topics instead we want one huge deck that mixes topics because block learning is inefficient. Learning Math, Math, Math, Math, History, History, History, History. is less efficient than learning Math, History, Math, History, Math, History ...

- [Why do people overestimate the effectiveness of blocked learning?](https://link.springer.com/article/10.3758/s13423-022-02225-7)
- [The Interleaving Effect: Mixing It Up Boosts Learning](https://www.scientificamerican.com/article/the-interleaving-effect-mixing-it-up-boosts-learning/#:~:text=With%20blocking%2C%20a%20single%20strategy,them%20into%20short%2Dterm%20memory.)

## [PROGESS] Stat Tracking

Tracking statistics is not only something needed to keep track of things but also because its simply loved by many. There should be a way to keep track of important stats and also an easy way to export them.

### Ideas

#### Study Progress Statistics

- Daily/Weekly/Monthly Review Counts: Track how many items were reviewed in each time period
- Accuracy Rates: Percentage of correct responses (Good/Easy vs Again)
- Study Streaks: Consecutive days of study
- Time Spent Studying: Track session duration and total study time

#### Spaced Repetition Metrics

- Card State Distribution: Number of cards in Learning/Review/Relearning states
- Retention Rate: Percentage of cards successfully remembered
- Stability Growth: Average stability increase over time
- Review Forecast: Visualization of upcoming reviews per day
- Average Difficulty: Track difficulty trends across all cards

#### Content Analysis

- Content Type Distribution: Statistics by item type (Flashcard, Quiz, Cloze, etc.)
- Tag Performance: Success rates by different tags
- Item Creation Rate: Tracking growth of your knowledge base
- Most Reviewed Items: Which items have been seen most frequently

#### Learning Insights

- Optimal Study Time: When your recall is highest based on time of day
- Forgetting Curve: Visualization of retention over time
- Learning Efficiency: How quickly items move from Learning to Review state
- Problematic Items: Cards with consistently low performance
