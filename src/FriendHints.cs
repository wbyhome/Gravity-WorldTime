using System;
using System.Collections.Generic;
using System.Linq;
namespace WorldTime;

// Full / short / narrow wording for English, Simplified Chinese,
// Traditional Chinese and Japanese. Narrow copy retains the situation.
public static class FriendHints {
 static readonly Dictionary<string,string[]> copy=new(){
 ["sleep"]=new[]{
  "Probably off in dreamland. Don't make the phone an alarm.~Probably asleep. Let the phone nap too.~Asleep?",
  "多半正和周公聊天，别让电话当闹钟。~多半睡得正香，电话先忍住。~多半熟睡",
  "多半正和周公聊天，別讓電話當鬧鐘。~多半睡得正香，電話先忍住。~多半熟睡",
  "たぶん夢の中。電話を目覚ましにしないでね。~たぶんぐっすり。電話は後に。~睡眠中かも"},
 ["waking"]=new[]{
  "They may still be negotiating with the duvet. No rush.~Maybe still snoozing. No rush.~Waking?",
  "可能正和被窝谈判，先别催着起床。~可能正和被窝谈判，先别催。~可能刚醒",
  "可能正和被窩談判，先別催著起床。~可能正和被窩談判，先別催。~可能剛醒",
  "まだ布団と交渉中かも。急かさずに。~まだうとうと。急かさずに。~起きた頃？"},
 ["weekend-bed"]=new[]{
  "A weekend lie-in may be in progress. Let the duvet win.~Weekend lie-in? Let them snooze.~Lie-in?",
  "周末可能还在赖床，让被窝再赢一会儿。~周末可能还在赖床，先别催。~可能赖床",
  "週末可能還在賴床，讓被窩再贏一會兒。~週末可能還在賴床，先別催。~可能賴床",
  "休日の朝寝坊中かも。もう少しそっと。~朝寝坊中かも。そっとしておこう。~朝寝坊かも"},
 ["breakfast"]=new[]{
  "Breakfast or the morning rush, perhaps. Send a little hello.~Breakfast rush? Send a hello.~Rushing?",
  "可能正吃早饭、赶路，先留句早安吧。~可能忙着早餐赶路，先留句早安。~可能赶路",
  "可能正吃早飯、趕路，先留句早安吧。~可能忙著早餐趕路，先留句早安。~可能趕路",
  "朝ごはんか通勤中かも。まずはおはようの一言を。~朝の支度中かも。まず一言。~支度中かも"},
 ["busy"]=new[]{
  "They might be in the middle of something. Send a little wave.~Might be busy. Send a little wave.~Busy?",
  "可能正忙着手头的事，先发消息探个头。~可能正忙着，发条消息探个头。~可能正忙",
  "可能正忙著手頭的事，先發訊息探個頭。~可能正忙著，發條訊息探個頭。~可能正忙",
  "何かに集中しているかも。まずは短いメッセージを。~忙しいかも。まずは一言。~忙しいかも"},
 ["lunch"]=new[]{
  "Lunch may have their full attention. Let them enjoy a bite.~Lunch, perhaps. Let them eat first.~Lunch?",
  "可能正和午饭较劲，让TA先吃口热的。~可能正吃午饭，吃饱再聊。~可能吃饭",
  "可能正和午飯較勁，讓TA先吃口熱的。~可能正吃午飯，吃飽再聊。~可能吃飯",
  "お昼ごはん中かも。まずはおいしく食べてもらおう。~お昼ごはん中かも。食後に。~食事中かも"},
 ["commute"]=new[]{
  "They may be heading home. Give them time to land.~Maybe heading home. No rush.~En route?",
  "可能刚下班、还在路上，等TA安顿下来。~可能还在下班路上，先别催。~可能在路上",
  "可能剛下班、還在路上，等TA安頓下來。~可能還在下班路上，先別催。~可能在路上",
  "帰り道かも。落ち着くまで待ってあげよう。~帰り道かも。急かさずに。~移動中かも"},
 ["dinner"]=new[]{
  "Dinner may be stealing the spotlight. Let them have a warm bite.~Dinner, perhaps. Leave a little hello.~Dinner?",
  "可能正扒拉晚饭，先让TA吃口热的。~可能正吃晚饭，先留句话。~可能吃晚饭",
  "可能正扒拉晚飯，先讓TA吃口熱的。~可能正吃晚飯，先留句話。~可能吃晚飯",
  "晩ごはんに夢中かも。まずは温かいうちに。~夕食中かも。まずは一言。~夕食中かも"},
 ["unwind"]=new[]{
  "They might be curled up relaxing. A little chat could fit.~Maybe unwinding. Ask about a chat.~Chilling?",
  "可能正窝着放松，聊两句或许刚刚好。~可能正窝着放松，问问能聊吗。~可能放松中",
  "可能正窩著放鬆，聊兩句或許剛剛好。~可能正窩著放鬆，問問能聊嗎。~可能放鬆中",
  "のんびりくつろいでいるかも。少し話せるかな。~くつろぎ中かも。話せるか聞こう。~休憩中かも"},
 ["bedtime"]=new[]{
  "They may be calling it a night. Save that ring for tomorrow.~Probably winding down. Call tomorrow.~Bedtime?",
  "多半准备关机睡觉了，电话留到明早吧。~多半准备睡了，电话明早再说。~可能要睡了",
  "多半準備關機睡覺了，電話留到明早吧。~多半準備睡了，電話明早再說。~可能要睡了",
  "そろそろ寝る頃かも。電話は明日にしよう。~そろそろ寝る頃かも。電話は明日。~寝る頃かも"},
 ["weekend"]=new[]{
  "They might be out enjoying the weekend. Say hi, no rush.~Out for the weekend? Say hi, no rush.~Out?",
  "周末可能正出门放风，发句问候，慢慢等回。~周末可能在放风，留言慢慢回。~可能在放风",
  "週末可能正出門放風，發句問候，慢慢等回。~週末可能在放風，留言慢慢回。~可能在放風",
  "休日のお出かけ中かも。気軽に一言、返事はゆっくり。~お出かけ中かも。返事はゆっくり。~外出中かも"},
 ["night-shift"]=new[]{
  "Your schedule says this could suit them. Ask before calling.~Within your schedule. Ask first.~Active?",
  "按你设的作息，这会儿也许正忙着，先问一句。~按所设作息，可能正忙着。~可能正忙",
  "按你設的作息，這會兒也許正忙著，先問一句。~按所設作息，可能正忙著。~可能正忙",
  "設定した時間帯では活動中かも。まず一言聞こう。~設定上は活動時間。まず確認を。~活動中かも"},
 ["weekend-wait"]=new[]{
  "No alarm-call today. Leave a message for when they're up.~Let them wake up, then reply.~Wait for a reply",
  "让TA睡到自然醒吧，醒了再回你。~先留言，等TA睡醒再回。~等睡醒再聊",
  "讓TA睡到自然醒吧，醒了再回你。~先留言，等TA睡醒再回。~等睡醒再聊",
  "今日は起こさずに。起きてから返事をもらおう。~起きてから返事をもらおう。~起きてから"},
 ["after-trip"]=new[]{
  "Leave a message; ask to chat once they're settled.~Message now; chat once settled.~Message first",
  "先留句话，等TA安顿好再约着聊。~先留言，等安顿好再聊。~先留言等回",
  "先留句話，等TA安頓好再約著聊。~先留言，等安頓好再聊。~先留言等回",
  "まず一言。落ち着いてから話す約束を。~落ち着いてから話そう。~まず一言"},
 ["after-meal"]=new[]{
  "Send a hello; ask if a chat after dinner would suit them.~Ask about a chat after the meal.~After eating?",
  "先留句话，问问吃完饭方便聊吗。~问问吃完饭方便聊吗。~饭后再约聊",
  "先留句話，問問吃完飯方便聊嗎。~問問吃完飯方便聊嗎。~飯後再約聊",
  "まず一言。食後に話せそうか聞いてみよう。~食後に話せるか聞こう。~食後に確認"},
 ["ask-first"]=new[]{
  "A quick 'free for a chat?' beats a surprise call.~Ask 'free to chat?' before calling.~Ask first",
  "先问一句方便吗，别让来电搞突然袭击。~先问方便吗，别突然来电。~先问方便吗",
  "先問一句方便嗎，別讓來電搞突然襲擊。~先問方便嗎，別突然來電。~先問方便嗎",
  "突然の電話より「今、話せる？」の一言を。~電話の前に「今、話せる？」と。~まず確認を"}
 };
 static int Language(string language)=>L10n.Resolve(language) switch{"zh-Hans"=>1,"zh-Hant"=>2,"ja"=>3,_=>0};
 public static string Get(string phase,string language,int length)=>copy[phase][Language(language)].Split('~')[Math.Clamp(length,0,2)];
 public static string When(string day,string language,int length,string time,string date){
  string[] text=length>=2?new[]{"Local "+time,"当地 "+time,"當地 "+time,"現地 "+time}:
   day=="today"?new[]{"Try gently after "+time+" their time.","等当地 "+time+"，再轻轻戳TA。","等當地 "+time+"，再輕輕戳TA。","現地 "+time+" 頃、そっと声をかけよう。"}:
   day=="tomorrow"?new[]{"Try tomorrow at "+time+" their time.","等当地明天 "+time+" 再戳TA。","等當地明天 "+time+" 再戳TA。","現地の明日 "+time+" 頃、声をかけよう。"}:
   new[]{"Next usual window: "+date+" "+time+" local.","按所设作息：当地 "+date+" "+time,"按所設作息：當地 "+date+" "+time,"設定の目安：現地 "+date+" "+time};
  return text[Language(language)];
 }
 public static bool Complete=>copy.Values.All(rows=>rows.Length==4&&rows.All(row=>row.Split('~').Length==3&&row.Split('~').All(text=>!string.IsNullOrWhiteSpace(text))));
}
