from colorist import Color


class ArtAsciiDraw:

    @staticmethod
    def banner() -> None:
        """
        This function, create the tool banner
        :return:
        """
        print(f'''{Color.BLUE}
     __               __        ________    ___   __  ______  _______ 
    / /_  _________  / /_____  / ____/ /   /   | / / / / __ \\/  _/   |
   / __ \\/ ___/ __ \\/ //_/ _ \\/ /   / /   / /| |/ / / / / / // // /| |
  / /_/ / /  / /_/ / ,< /  __/ /___/ /___/ ___ / /_/ / /_/ // // ___ |
 /_.___/_/   \\____/_/|_|\\___/\\____/_____/_/  |_\\____/_____/___/_/  |_|
                                                 {Color.RED}By TheMalwareGuardian{Color.BLUE}  

 brokeCLAUDIA - Broken access control vulnerability in microCLAUDIA
        {Color.OFF}''')

    @staticmethod
    def vulnerable() -> None:
        """
        This function, draw an open door when the vulnerability exists
        :return:
        """
        print(f'''{Color.GREEN}
  ______________
 |\\ ___________ /|
 | |  /|,| |   | |
 | | |,x,| |   | |
 | | |,x,' |   | |
 | | |,x   ,   | |
 | | |/    |   | |
 | |    /] ,   | |
 | |   [/ ()   | |
 | |       |   | |
 | |       |   | |
 | |       |   | |
 | |      ,'   | |
 | |   ,'      | |
 |_|,'_________|_| VULNERABLE
        {Color.OFF}''')
        print('\n')

    @staticmethod
    def not_vulnerable() -> None:
        """
        This function, draw a closed door when the vulnerability doesn't exist
        :return:
        """
        print(f'''{Color.RED}
  ______________
 |\\ ___________ /|
 | |  _ _ _ _  | |
 | | | | | | | | |
 | | |-+-+-+-| | |
 | | |-+-+=+%| | |
 | | |_|_|_|_| | |
 | |    ___    | |
 | |   [___] ()| |
 
 | |         ||| |
 | |         ()| |
 | |           | |
 | |           | |
 | |           | |
 |_|___________|_| NOT VULNERABLE
        {Color.OFF}''')
        print('\n')
