#!/usr/bin/php
<?php

  /**
   *   GENERATES UPDATE SCRIPT FOR MINECRAFT LIBRARIES
   *   
   *   WORKS WITH 1.8+
   **/
 
  if (!@$argv[1]) die("Usage: {$argv[0]} <version> [directory prefix]\n");
  
  function printl($t) { fwrite(STDERR, $t); }
  
  @$dir_prefix = $argv[2];
  if (!$dir_prefix) $dir_prefix = "";
  else if (substr($dir_prefix, -1) != "/") $dir_prefix = $dir_prefix."/";
  
  
  printl("Downloading version json ({$argv[1]})... ");
  $version = $argv[1];
  $info = json_decode(file_get_contents("http://s3.amazonaws.com/Minecraft.Download/versions/$version/$version.json"), true);

  if ($dir_prefix) echo "CANFAIL MKDIR \"$dir_prefix\"\n";
  
  foreach ($info["libraries"] as $lib)
  {
    // We're not including twitch libraries.
    if (strpos($lib["name"], "twitch")) continue;
    $cprefix = "";
    $os = 3;
    $allow_win = true;
    $allow_loonix = true;
    if (isset($lib["rules"]))
    {
      $allow_win = false;
      $allow_loonix = false;
      foreach ($lib["rules"] as $rule)
      {
         if (isset($rule["os"]))
         {
           foreach ($rule["os"] as $os)
           {
             if ($os == "osx") continue;
             if ($os == "linux")
             {
               if ($rule["action"] == "allow") $allow_loonix = true;
               else if ($rule["action"] == "disallow") $allow_loonix = false;
             }
             if ($os == "windows")
             {
               if ($rule["action"] == "allow") $allow_win = true;
               else if ($rule["action"] == "disallow") $allow_win = false;
             }
           }
         }
         else
         {
           if ($rule["action"] == "allow") 
           {
             $allow_loonix = true;
             $allow_win = true;
           }
           else 
           {
             $allow_loonix = false;
             $allow_win = false;
           }
         }
      }
    }
    if ((!$allow_win) && (!$allow_loonix)) continue;
    $prefix = "";
    if ((!$allow_win) || (!$allow_loonix))
    {
      if ($allow_loonix) $prefix = "IFLOONIX ";
      else if ($allow_win) $prefix = "IFWIN ";
    }
    $downloads = $lib["downloads"];
    if (isset($downloads["artifact"]))
    {
      echo "{$prefix}OVERRIDE \"{$dir_prefix}".basename($downloads["artifact"]["path"])."\" \"{$downloads["artifact"]["url"]}\"\n";
      echo "{$prefix}ADD \"{$dir_prefix}".basename($downloads["artifact"]["path"])."\" \"{$downloads["artifact"]["sha1"]}\"\n";
    }
    else if (isset($downloads["classifiers"]))
    {
      foreach ($downloads["classifiers"] as $os => $natives)
      {
        if ($os == "natives-linux") $prefix = "IFLOONIX ";
        else if ($os == "natives-windows") $prefix = "IFWIN ";
        else 
        {
          printl("Skipping $os in {$lib["name"]}...\n");
          continue;
        }
        printl("Downloading library {$lib["name"]} for $os...\n");
        $file = tempnam(sys_get_temp_dir(), "mclib");
        file_put_contents($file, file_get_contents($natives["url"]));
        $filelist = explode("\n", shell_exec("unzip -Z1 $file"));
        unlink($file);
        if (!count($filelist))
        {
          printl("ERROR: Filelist is empty, something wrong perhaps?\n");
          die();
        }
        $lib["name"] = str_replace(":", "-", $lib["name"]); // Dumb windows
        echo "{$prefix}OVERRIDE \"templib_{$lib["name"]}\" \"{$natives["url"]}\"\n";
        echo "{$prefix}ADD \"templib_{$lib["name"]}\" \"{$natives["sha1"]}\"\n";
        foreach ($filelist as $file)
        {
          if ((substr($file, 0, 8) == "META-INF") || (!$file)) continue;
          echo "{$prefix}EXTRACT \"templib_{$lib["name"]}\" \"$file\" \"{$dir_prefix}$file\"\n";
        }
        echo "{$prefix}DEL \"templib_{$lib["name"]}\"\n";
      }
    }
  }