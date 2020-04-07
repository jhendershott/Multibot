class Ranks{

    rankList = ['RCT', 'CDT', 'PVT', 'PV2', 'PFC', 'SPC', 'LCPL', 'CPL', 'SGT', 'SSG', 'LT2', 'LT', 'LCDR', 'CDR', 'CAPT', 'VADM', 'ADM', 'HADM']

    rankAbbrevs = [
        {rank: 'Recruit', abbrev: 'RCT', num: 0 },
        {rank: 'Cadet', abbrev: 'CDT', num: 1  },
        {rank: 'Private', abbrev: 'PVT', num: 2  },
        {rank: 'Private Second Class', abbrev: 'PV2', num: 3 },
        {rank: 'Private First Class', abbrev: 'PFC', num: 4 },
        {rank: 'Specialist', abbrev: 'SPC', num: 5 },
        {rank: 'Lance Corporal', abbrev: 'LCPL', num: 6 },
        {rank: 'Corporal', abbrev: 'CPL', num: 7 },
        {rank: 'Sergeant', abbrev: 'SGT' , num: 8 },
        {rank: 'Staff Sergeant', abbrev: 'SSG' , num: 9 },
        {rank: 'Second Lieutenant', abbrev: 'LT2' , num: 10 },
        {rank: 'Lieutenant', abbrev: 'LT' , num: 11 },
        {rank: 'Lieutenant Commander', abbrev: 'LCDR' , num: 12 },
        {rank: 'Commander', abbrev: 'CDR' , num: 13 },
        {rank: 'Captain', abbrev: 'CPT' , num: 14 },
        {rank: 'Vice Admiral', abbrev: 'VADM' , num: 15 },
        {rank: 'Admiral', abbrev: 'ADM' , num: 16 },
        {rank: 'High Admiral', abbrev: 'HADM' , num: 17 },
    ]

    
    ClearAllRanks(nick){
        for(var i = 0; i < this.rankAbbrevs.length; i++){
        nick = nick.replace(`${this.rankAbbrevs[i].abbrev}. `, '');
        }

        return nick
    }

    RankByNumber(num){
        for(var i = 0; i < this.rankAbbrevs.length; i++){
            if(this.rankAbbrevs[i].num === num){
                return this.rankAbbrevs[i]
            }
        }
    }

    GetRank(rankName){
        for(var i = 0; i < this.rankAbbrevs.length; i++){
          if(rankName.toUpperCase() === this.rankAbbrevs[i].abbrev){
            return this.rankAbbrevs[i]
          }
        }
    
        return null;
      }

    HasRank(nick){
        for(var i = 0; i< this.rankAbbrevs.length; i++){
            let nsplit = nick.split(/ +/);;
            if(nsplit[0] === `${this.rankAbbrevs[i].abbrev}.`){
            return this.rankAbbrevs[i]
            }
        }
        return null;
    }
}

module.exports = Ranks;